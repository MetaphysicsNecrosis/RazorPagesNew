using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using System.Text;

namespace RazorPagesNew.Pages.Employees
{
    public class ImportTemplateModel : PageModel
    {
        private readonly MyApplicationDbContext _context;

        public ImportTemplateModel(MyApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string format)
        {
            format = format?.ToLowerInvariant();

            switch (format)
            {
                case "excel":
                    return GetExcelTemplate();
                case "word":
                    return GetWordTemplate();
                case "xml":
                    return GetXmlTemplate();
                default:
                    return BadRequest("Неподдерживаемый формат шаблона");
            }
        }

        private IActionResult GetExcelTemplate()
        {
            // Создаем простой CSV-файл, который можно открыть в Excel
            var departments = _context.Departments.ToList();
            var departmentNames = string.Join(", ", departments.Select(d => d.Name));

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("ФИО,Email,Телефон,Должность,Отдел,ДатаПриема");
            csvBuilder.AppendLine("Иванов Иван Иванович,ivanov@example.com,+7 999 123-45-67,Ведущий специалист,IT,01.01.2022");
            csvBuilder.AppendLine("Петрова Екатерина Сергеевна,petrova@example.com,+7 999 234-56-78,Менеджер,HR,15.03.2022");
            csvBuilder.AppendLine($",,,,\"Доступные отделы: {departmentNames}\",");

            var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(bytes, "text/csv", "employees_import_template.csv");
        }

        private IActionResult GetWordTemplate()
        {
            // Создаем HTML-файл, который можно сохранить как .docx
            var departments = _context.Departments.ToList();
            var departmentNames = string.Join(", ", departments.Select(d => d.Name));

            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html>");
            htmlBuilder.AppendLine("<head><title>Шаблон импорта сотрудников</title></head>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("<h1>Шаблон импорта сотрудников</h1>");
            htmlBuilder.AppendLine("<p>Заполните таблицу данными о сотрудниках и сохраните как документ Word (.docx)</p>");

            htmlBuilder.AppendLine("<table border='1' cellpadding='5'>");
            htmlBuilder.AppendLine("<tr><th>ФИО</th><th>Email</th><th>Телефон</th><th>Должность</th><th>Отдел</th><th>ДатаПриема</th></tr>");
            htmlBuilder.AppendLine("<tr><td>Иванов Иван Иванович</td><td>ivanov@example.com</td><td>+7 999 123-45-67</td><td>Ведущий специалист</td><td>IT</td><td>01.01.2022</td></tr>");
            htmlBuilder.AppendLine("<tr><td>Петрова Екатерина Сергеевна</td><td>petrova@example.com</td><td>+7 999 234-56-78</td><td>Менеджер</td><td>HR</td><td>15.03.2022</td></tr>");
            htmlBuilder.AppendLine("</table>");

            htmlBuilder.AppendLine("<p><strong>Примечание:</strong> Доступные отделы: " + departmentNames + "</p>");

            htmlBuilder.AppendLine("<h2>Альтернативный формат</h2>");
            htmlBuilder.AppendLine("<p>Можно также использовать текстовый формат:</p>");

            htmlBuilder.AppendLine("<pre>");
            htmlBuilder.AppendLine("--------- Сотрудник 1 ---------");
            htmlBuilder.AppendLine("ФИО: Иванов Иван Иванович");
            htmlBuilder.AppendLine("Email: ivanov@example.com");
            htmlBuilder.AppendLine("Телефон: +7 999 123-45-67");
            htmlBuilder.AppendLine("Должность: Ведущий специалист");
            htmlBuilder.AppendLine("Отдел: IT");
            htmlBuilder.AppendLine("ДатаПриема: 01.01.2022");
            htmlBuilder.AppendLine();
            htmlBuilder.AppendLine("--------- Сотрудник 2 ---------");
            htmlBuilder.AppendLine("ФИО: Петрова Екатерина Сергеевна");
            htmlBuilder.AppendLine("Email: petrova@example.com");
            htmlBuilder.AppendLine("Телефон: +7 999 234-56-78");
            htmlBuilder.AppendLine("Должность: Менеджер");
            htmlBuilder.AppendLine("Отдел: HR");
            htmlBuilder.AppendLine("ДатаПриема: 15.03.2022");
            htmlBuilder.AppendLine("</pre>");

            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            var bytes = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            return File(bytes, "text/html", "employees_import_template.html");
        }

        private IActionResult GetXmlTemplate()
        {
            // Создаем простой XML-файл в формате 1C
            var departments = _context.Departments.ToList();

            var xmlBuilder = new StringBuilder();
            xmlBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xmlBuilder.AppendLine("<Справочник.Сотрудники>");

            // Добавляем примеры сотрудников
            xmlBuilder.AppendLine("  <Сотрудник>");
            xmlBuilder.AppendLine("    <ФИО>Иванов Иван Иванович</ФИО>");
            xmlBuilder.AppendLine("    <Email>ivanov@example.com</Email>");
            xmlBuilder.AppendLine("    <Телефон>+7 999 123-45-67</Телефон>");
            xmlBuilder.AppendLine("    <Должность>Ведущий специалист</Должность>");
            xmlBuilder.AppendLine("    <Отдел>IT</Отдел>");
            xmlBuilder.AppendLine("    <ДатаПриема>2022-01-01</ДатаПриема>");
            xmlBuilder.AppendLine("  </Сотрудник>");

            xmlBuilder.AppendLine("  <Сотрудник>");
            xmlBuilder.AppendLine("    <ФИО>Петрова Екатерина Сергеевна</ФИО>");
            xmlBuilder.AppendLine("    <Email>petrova@example.com</Email>");
            xmlBuilder.AppendLine("    <Телефон>+7 999 234-56-78</Телефон>");
            xmlBuilder.AppendLine("    <Должность>Менеджер</Должность>");
            xmlBuilder.AppendLine("    <Отдел>HR</Отдел>");
            xmlBuilder.AppendLine("    <ДатаПриема>2022-03-15</ДатаПриема>");
            xmlBuilder.AppendLine("  </Сотрудник>");

            // Добавляем комментарий с доступными отделами
            xmlBuilder.AppendLine("  <!-- Доступные отделы: ");
            foreach (var department in departments)
            {
                xmlBuilder.AppendLine($"    - {department.Name}");
            }
            xmlBuilder.AppendLine("  -->");

            xmlBuilder.AppendLine("</Справочник.Сотрудники>");

            var bytes = Encoding.UTF8.GetBytes(xmlBuilder.ToString());
            return File(bytes, "application/xml", "employees_import_template.xml");
        }
    }
}