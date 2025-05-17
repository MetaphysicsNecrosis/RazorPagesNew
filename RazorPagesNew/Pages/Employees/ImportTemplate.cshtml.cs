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
                    return BadRequest("���������������� ������ �������");
            }
        }

        private IActionResult GetExcelTemplate()
        {
            // ������� ������� CSV-����, ������� ����� ������� � Excel
            var departments = _context.Departments.ToList();
            var departmentNames = string.Join(", ", departments.Select(d => d.Name));

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("���,Email,�������,���������,�����,����������");
            csvBuilder.AppendLine("������ ���� ��������,ivanov@example.com,+7 999 123-45-67,������� ����������,IT,01.01.2022");
            csvBuilder.AppendLine("������� ��������� ���������,petrova@example.com,+7 999 234-56-78,��������,HR,15.03.2022");
            csvBuilder.AppendLine($",,,,\"��������� ������: {departmentNames}\",");

            var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(bytes, "text/csv", "employees_import_template.csv");
        }

        private IActionResult GetWordTemplate()
        {
            // ������� HTML-����, ������� ����� ��������� ��� .docx
            var departments = _context.Departments.ToList();
            var departmentNames = string.Join(", ", departments.Select(d => d.Name));

            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html>");
            htmlBuilder.AppendLine("<head><title>������ ������� �����������</title></head>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("<h1>������ ������� �����������</h1>");
            htmlBuilder.AppendLine("<p>��������� ������� ������� � ����������� � ��������� ��� �������� Word (.docx)</p>");

            htmlBuilder.AppendLine("<table border='1' cellpadding='5'>");
            htmlBuilder.AppendLine("<tr><th>���</th><th>Email</th><th>�������</th><th>���������</th><th>�����</th><th>����������</th></tr>");
            htmlBuilder.AppendLine("<tr><td>������ ���� ��������</td><td>ivanov@example.com</td><td>+7 999 123-45-67</td><td>������� ����������</td><td>IT</td><td>01.01.2022</td></tr>");
            htmlBuilder.AppendLine("<tr><td>������� ��������� ���������</td><td>petrova@example.com</td><td>+7 999 234-56-78</td><td>��������</td><td>HR</td><td>15.03.2022</td></tr>");
            htmlBuilder.AppendLine("</table>");

            htmlBuilder.AppendLine("<p><strong>����������:</strong> ��������� ������: " + departmentNames + "</p>");

            htmlBuilder.AppendLine("<h2>�������������� ������</h2>");
            htmlBuilder.AppendLine("<p>����� ����� ������������ ��������� ������:</p>");

            htmlBuilder.AppendLine("<pre>");
            htmlBuilder.AppendLine("--------- ��������� 1 ---------");
            htmlBuilder.AppendLine("���: ������ ���� ��������");
            htmlBuilder.AppendLine("Email: ivanov@example.com");
            htmlBuilder.AppendLine("�������: +7 999 123-45-67");
            htmlBuilder.AppendLine("���������: ������� ����������");
            htmlBuilder.AppendLine("�����: IT");
            htmlBuilder.AppendLine("����������: 01.01.2022");
            htmlBuilder.AppendLine();
            htmlBuilder.AppendLine("--------- ��������� 2 ---------");
            htmlBuilder.AppendLine("���: ������� ��������� ���������");
            htmlBuilder.AppendLine("Email: petrova@example.com");
            htmlBuilder.AppendLine("�������: +7 999 234-56-78");
            htmlBuilder.AppendLine("���������: ��������");
            htmlBuilder.AppendLine("�����: HR");
            htmlBuilder.AppendLine("����������: 15.03.2022");
            htmlBuilder.AppendLine("</pre>");

            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            var bytes = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            return File(bytes, "text/html", "employees_import_template.html");
        }

        private IActionResult GetXmlTemplate()
        {
            // ������� ������� XML-���� � ������� 1C
            var departments = _context.Departments.ToList();

            var xmlBuilder = new StringBuilder();
            xmlBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xmlBuilder.AppendLine("<����������.����������>");

            // ��������� ������� �����������
            xmlBuilder.AppendLine("  <���������>");
            xmlBuilder.AppendLine("    <���>������ ���� ��������</���>");
            xmlBuilder.AppendLine("    <Email>ivanov@example.com</Email>");
            xmlBuilder.AppendLine("    <�������>+7 999 123-45-67</�������>");
            xmlBuilder.AppendLine("    <���������>������� ����������</���������>");
            xmlBuilder.AppendLine("    <�����>IT</�����>");
            xmlBuilder.AppendLine("    <����������>2022-01-01</����������>");
            xmlBuilder.AppendLine("  </���������>");

            xmlBuilder.AppendLine("  <���������>");
            xmlBuilder.AppendLine("    <���>������� ��������� ���������</���>");
            xmlBuilder.AppendLine("    <Email>petrova@example.com</Email>");
            xmlBuilder.AppendLine("    <�������>+7 999 234-56-78</�������>");
            xmlBuilder.AppendLine("    <���������>��������</���������>");
            xmlBuilder.AppendLine("    <�����>HR</�����>");
            xmlBuilder.AppendLine("    <����������>2022-03-15</����������>");
            xmlBuilder.AppendLine("  </���������>");

            // ��������� ����������� � ���������� ��������
            xmlBuilder.AppendLine("  <!-- ��������� ������: ");
            foreach (var department in departments)
            {
                xmlBuilder.AppendLine($"    - {department.Name}");
            }
            xmlBuilder.AppendLine("  -->");

            xmlBuilder.AppendLine("</����������.����������>");

            var bytes = Encoding.UTF8.GetBytes(xmlBuilder.ToString());
            return File(bytes, "application/xml", "employees_import_template.xml");
        }
    }
}