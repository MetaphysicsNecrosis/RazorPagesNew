﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Implementation
{
    public class RoleSynchronizationService : IRoleSynchronizationService
    {
        private readonly RoleManager<IdentityRole> _identityRoleManager;
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly MyApplicationDbContext _dbContext;

        public RoleSynchronizationService(
            RoleManager<IdentityRole> identityRoleManager,
            UserManager<IdentityUser> identityUserManager,
            MyApplicationDbContext dbContext)
        {
            _identityRoleManager = identityRoleManager;
            _identityUserManager = identityUserManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Синхронизирует роли между Identity и кастомной системой
        /// </summary>
        public async Task SynchronizeRolesAsync()
        {
            // 1. Получаем все роли из Identity
            var identityRoles = await _identityRoleManager.Roles.ToListAsync();

            // 2. Получаем все роли из кастомной системы
            var customRoles = await _dbContext.Roles.ToListAsync();

            // 3. Создаем отсутствующие роли в Identity
            foreach (var customRole in customRoles)
            {
                if (!identityRoles.Any(r => r.Name == customRole.Name))
                {
                    await _identityRoleManager.CreateAsync(new IdentityRole(customRole.Name));
                }
            }

            // 4. Создаем отсутствующие роли в кастомной системе
            foreach (var identityRole in identityRoles)
            {
                if (!customRoles.Any(r => r.Name == identityRole.Name))
                {
                    var newCustomRole = new Role
                    {
                        Name = identityRole.Name
                    };

                    await _dbContext.Roles.AddAsync(newCustomRole);
                }
            }

            // Сохраняем изменения
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Синхронизирует роли пользователя между Identity и кастомной системой
        /// </summary>
        public async Task SynchronizeUserRolesAsync(string identityUserId)
        {
            // 1. Получаем пользователя Identity
            var identityUser = await _identityUserManager.FindByIdAsync(identityUserId);
            if (identityUser == null) return;

            // 2. Получаем кастомного пользователя
            var customUser = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (customUser == null) return;

            // 3. Получаем роли пользователя в Identity
            var identityRoles = await _identityUserManager.GetRolesAsync(identityUser);

            // 4. Если у пользователя в Identity есть роли, но нет кастомной роли,
            // назначаем ему первую роль из Identity
            if (identityRoles.Any() && customUser.RoleId == 0)
            {
                var firstRoleName = identityRoles.First();
                var customRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == firstRoleName);

                if (customRole != null)
                {
                    customUser.RoleId = customRole.Id;
                    await _dbContext.SaveChangesAsync();
                }
            }
            // 5. Если у пользователя есть кастомная роль, но нет ролей в Identity,
            // добавляем кастомную роль в Identity
            else if (!identityRoles.Any() && customUser.RoleId > 0)
            {
                var roleName = customUser.Role.Name;
                await _identityUserManager.AddToRoleAsync(identityUser, roleName);
            }
        }

        /// <summary>
        /// Добавляет роль пользователю в обеих системах
        /// </summary>
        public async Task AddUserToRoleAsync(string identityUserId, string roleName)
        {
            // 1. Получаем пользователя Identity
            var identityUser = await _identityUserManager.FindByIdAsync(identityUserId);
            if (identityUser == null) return;

            // 2. Получаем кастомного пользователя
            var customUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (customUser == null) return;

            // 3. Получаем (или создаем) роль в Identity
            var identityRole = await _identityRoleManager.FindByNameAsync(roleName);
            if (identityRole == null)
            {
                var createResult = await _identityRoleManager.CreateAsync(new IdentityRole(roleName));
                if (!createResult.Succeeded) return;

                identityRole = await _identityRoleManager.FindByNameAsync(roleName);
            }

            // 4. Получаем (или создаем) кастомную роль
            var customRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (customRole == null)
            {
                customRole = new Role { Name = roleName };
                await _dbContext.Roles.AddAsync(customRole);
                await _dbContext.SaveChangesAsync();
            }

            // 5. Добавляем роль пользователю в Identity
            await _identityUserManager.AddToRoleAsync(identityUser, roleName);

            // 6. Обновляем роль пользователя в кастомной системе
            customUser.RoleId = customRole.Id;
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Удаляет роль у пользователя в обеих системах
        /// </summary>
        public async Task RemoveUserFromRoleAsync(string identityUserId, string roleName)
        {
            // 1. Получаем пользователя Identity
            var identityUser = await _identityUserManager.FindByIdAsync(identityUserId);
            if (identityUser == null) return;

            // 2. Получаем кастомного пользователя
            var customUser = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (customUser == null) return;

            // 3. Удаляем роль у пользователя в Identity
            await _identityUserManager.RemoveFromRoleAsync(identityUser, roleName);

            // 4. Если текущая роль пользователя в кастомной системе совпадает с удаляемой,
            // назначаем ему другую роль (если есть) или роль по умолчанию
            if (customUser.Role != null && customUser.Role.Name == roleName)
            {
                // Получаем оставшиеся роли пользователя в Identity
                var remainingRoles = await _identityUserManager.GetRolesAsync(identityUser);

                if (remainingRoles.Any())
                {
                    // Назначаем первую оставшуюся роль
                    var newRoleName = remainingRoles.First();
                    var newRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == newRoleName);

                    if (newRole != null)
                    {
                        customUser.RoleId = newRole.Id;
                    }
                }
                else
                {
                    // Назначаем роль по умолчанию (например, "User")
                    var defaultRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");

                    if (defaultRole != null)
                    {
                        customUser.RoleId = defaultRole.Id;
                        // Добавляем роль по умолчанию в Identity
                        await _identityUserManager.AddToRoleAsync(identityUser, "User");
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Синхронизирует всех пользователей между Identity и кастомной системой
        /// </summary>
        public async Task SynchronizeUsersAsync()
        {
            // 1. Получаем всех пользователей из обеих систем
            var identityUsers = await _identityUserManager.Users.ToListAsync();
            var customUsers = await _dbContext.Users.Include(u => u.Role).ToListAsync();

            // 2. Для каждого пользователя из Identity проверяем наличие в кастомной системе
            foreach (var identityUser in identityUsers)
            {
                bool isSkip = false;
                var customUser = customUsers.FirstOrDefault(u => u.IdentityUserId.Trim() == identityUser.Id);
                /*foreach (var cuser in customUsers) 
                {
                    if(String.Equals(cuser.IdentityUserId.Trim(),identityUser.Id)) isSkip = true;
                }
                if (isSkip) continue;*/
                
                // Если пользователя нет в кастомной системе, создаем его
                if (customUser == null)
                {
                    // Получаем роли пользователя из Identity
                    var userRoles = await _identityUserManager.GetRolesAsync(identityUser);

                    // Определяем основную роль - первую из списка или "User" по умолчанию
                    string mainRoleName = userRoles.Any() ? userRoles.First() : "User";

                    // Находим или создаем соответствующую роль в кастомной системе
                    var customRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == mainRoleName);
                    if (customRole == null)
                    {
                        customRole = new Role { Name = mainRoleName };
                        await _dbContext.Roles.AddAsync(customRole);
                        await _dbContext.SaveChangesAsync();
                    }

                    // Создаем пользователя в кастомной системе
                    customUser = new User
                    {
                        Username = identityUser.UserName,
                        IdentityUserId = identityUser.Id,
                        RoleId = customRole.Id,
                        PasswordHash = "MANAGED_BY_IDENTITY" // пароль хранится в Identity
                    };

                    await _dbContext.Users.AddAsync(customUser);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // Синхронизируем роли существующего пользователя
                    await SynchronizeUserRolesAsync(identityUser.Id);
                }
            }

            // 3. Для каждого пользователя из кастомной системы проверяем наличие в Identity
            foreach (var customUser in customUsers)
            {
                // Если у пользователя нет привязки к Identity или пользователь не найден в Identity
                if (string.IsNullOrEmpty(customUser.IdentityUserId) ||
                    !identityUsers.Any(u => u.Id == customUser.IdentityUserId))
                {
                    // Создаем нового пользователя в Identity
                    var newIdentityUser = new IdentityUser
                    {
                        UserName = customUser.Username,
                        Email = customUser.Username // Предполагаем, что Username это email
                    };

                    // Генерируем временный пароль
                    string tempPassword = GenerateRandomPassword();
                    var result = await _identityUserManager.CreateAsync(newIdentityUser, tempPassword);

                    if (result.Succeeded)
                    {
                        // Добавляем пользователю роль из кастомной системы
                        if (customUser.Role != null)
                        {
                            await _identityUserManager.AddToRoleAsync(newIdentityUser, customUser.Role.Name);
                        }

                        // Обновляем ссылку на Identity в кастомном пользователе
                        customUser.IdentityUserId = newIdentityUser.Id;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Генерирует случайный пароль для новых пользователей
        /// </summary>
        private string GenerateRandomPassword(int length = 12)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();

            // Генерируем случайную строку
            var password = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return password;
        }
    }
}