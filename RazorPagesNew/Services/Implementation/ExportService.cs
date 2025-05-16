using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml;
using RazorPagesNew.ModelsDb.Reports;
using RazorPagesNew.Services.Interfaces;
using System.Drawing.Printing;
using System.Xml.Linq;
using System.Xml;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace RazorPagesNew.Services.Implementation
{
    public class ExportService : IExportService
    {
        /// <summary>
        /// Экспорт отчета в формат PDF
        /// </summary>
        public async Task<byte[]> ExportToPdfAsync(IReport report)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // Добавление заголовка
                Font titleFont = new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD);
                Paragraph title = new Paragraph(report.Title, titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);

                // Добавление даты
                Font dateFont = new Font(Font.FontFamily.HELVETICA, 12, Font.ITALIC);
                Paragraph date = new Paragraph($"Generated: {report.GeneratedAt.ToString("dd.MM.yyyy HH:mm")}", dateFont);
                date.Alignment = Element.ALIGN_RIGHT;
                document.Add(date);

                document.Add(new Paragraph(" ")); // Пустая строка

                // В зависимости от типа отчета, добавляем соответствующий контент
                switch (report.Type)
                {
                    case ReportType.EmployeeReport:
                        await AddEmployeeReportContentPdf(document, (EmployeeReport)report);
                        break;
                    case ReportType.DepartmentReport:
                        await AddDepartmentReportContentPdf(document, (DepartmentReport)report);
                        break;
                    case ReportType.CriteriaAnalysis:
                        await AddCriteriaAnalysisReportContentPdf(document, (CriteriaAnalysisReport)report);
                        break;
                    case ReportType.PerformanceTrend:
                        await AddPerformanceTrendReportContentPdf(document, (PerformanceTrendReport)report);
                        break;
                    case ReportType.Attendance:
                        await AddAttendanceReportContentPdf(document, (AttendanceReport)report);
                        break;
                    case ReportType.Analytical:
                        await AddAnalyticalReportContentPdf(document, (AnalyticalReport)report);
                        break;
                }

                document.Close();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Экспорт отчета в формат Excel
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(IReport report)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                // В зависимости от типа отчета, добавляем соответствующий контент
                switch (report.Type)
                {
                    case ReportType.EmployeeReport:
                        await AddEmployeeReportContentExcel(package, (EmployeeReport)report);
                        break;
                    case ReportType.DepartmentReport:
                        await AddDepartmentReportContentExcel(package, (DepartmentReport)report);
                        break;
                    case ReportType.CriteriaAnalysis:
                        await AddCriteriaAnalysisReportContentExcel(package, (CriteriaAnalysisReport)report);
                        break;
                    case ReportType.PerformanceTrend:
                        await AddPerformanceTrendReportContentExcel(package, (PerformanceTrendReport)report);
                        break;
                    case ReportType.Attendance:
                        await AddAttendanceReportContentExcel(package, (AttendanceReport)report);
                        break;
                    case ReportType.Analytical:
                        await AddAnalyticalReportContentExcel(package, (AnalyticalReport)report);
                        break;
                }

                return package.GetAsByteArray();
            }
        }

        /// <summary>
        /// Экспорт отчета в формат XML
        /// </summary>
        public async Task<byte[]> ExportToXmlAsync(IReport report)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace
                };

                using (XmlWriter writer = XmlWriter.Create(ms, settings))
                {
                    // В зависимости от типа отчета, создаем соответствующую XML структуру
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Report");

                    writer.WriteElementString("Title", report.Title);
                    writer.WriteElementString("GeneratedAt", report.GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ss"));
                    writer.WriteElementString("Type", report.Type.ToString());

                    switch (report.Type)
                    {
                        case ReportType.EmployeeReport:
                            await AddEmployeeReportContentXml(writer, (EmployeeReport)report);
                            break;
                        case ReportType.DepartmentReport:
                            await AddDepartmentReportContentXml(writer, (DepartmentReport)report);
                            break;
                        case ReportType.CriteriaAnalysis:
                            await AddCriteriaAnalysisReportContentXml(writer, (CriteriaAnalysisReport)report);
                            break;
                        case ReportType.PerformanceTrend:
                            await AddPerformanceTrendReportContentXml(writer, (PerformanceTrendReport)report);
                            break;
                        case ReportType.Attendance:
                            await AddAttendanceReportContentXml(writer, (AttendanceReport)report);
                            break;
                        case ReportType.Analytical:
                            await AddAnalyticalReportContentXml(writer, (AnalyticalReport)report);
                            break;
                    }

                    writer.WriteEndElement(); // Report
                    writer.WriteEndDocument();
                }

                return ms.ToArray();
            }
        }

        #region PDF Export Methods

        /// <summary>
        /// Добавление содержимого индивидуального отчета в PDF
        /// </summary>
        private async Task AddEmployeeReportContentPdf(Document document, EmployeeReport report)
        {
            // Информация о сотруднике
            Font sectionFont = new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD);
            document.Add(new Paragraph("Employee Information", sectionFont));

            Font contentFont = new Font(Font.FontFamily.HELVETICA, 12);
            document.Add(new Paragraph($"Name: {report.Employee.FullName}", contentFont));
            document.Add(new Paragraph($"Position: {report.Employee.Position}", contentFont));
            document.Add(new Paragraph($"Department: {report.Employee.Department.Name}", contentFont));
            document.Add(new Paragraph($"Hire Date: {report.Employee.HireDate.ToString("dd.MM.yyyy")}", contentFont));

            document.Add(new Paragraph(" ")); // Пустая строка

            // Общая оценка
            document.Add(new Paragraph("Performance Summary", sectionFont));
            document.Add(new Paragraph($"Average Score: {report.AverageScore:F2} of 5.0", contentFont));
            document.Add(new Paragraph($"Tasks Completed: {report.TaskStats.CompletedTasks}", contentFont));
            document.Add(new Paragraph($"Average Task Efficiency: {report.TaskStats.AverageEfficiency:F2}%", contentFont));
            document.Add(new Paragraph($"Attendance Rate: {report.AttendanceStats.AttendanceRate:P2}", contentFont));

            document.Add(new Paragraph(" ")); // Пустая строка

            // Таблица оценок
            document.Add(new Paragraph("Evaluation History", sectionFont));

            PdfPTable table = new PdfPTable(4);
            table.WidthPercentage = 100;

            // Заголовки
            Font headerFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
            table.AddCell(new PdfPCell(new Phrase("Date", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Evaluator", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Score", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Notes", headerFont)));

            // Данные
            foreach (var eval in report.Evaluations)
            {
                table.AddCell(new PdfPCell(new Phrase(eval.EvaluationDate.ToString("dd.MM.yyyy"), contentFont)));
                table.AddCell(new PdfPCell(new Phrase(eval.Evaluator.Username, contentFont)));
                table.AddCell(new PdfPCell(new Phrase(eval.Score.ToString("F2"), contentFont)));
                table.AddCell(new PdfPCell(new Phrase(eval.Notes, contentFont)));
            }

            document.Add(table);
            document.Add(new Paragraph(" ")); // Пустая строка

            // Таблица оценок по критериям
            document.Add(new Paragraph("Evaluation by Criteria", sectionFont));

            if (report.CriteriaScores.Any())
            {
                PdfPTable criteriaTable = new PdfPTable(3);
                criteriaTable.WidthPercentage = 100;

                // Заголовки
                criteriaTable.AddCell(new PdfPCell(new Phrase("Criterion", headerFont)));
                criteriaTable.AddCell(new PdfPCell(new Phrase("Score", headerFont)));
                criteriaTable.AddCell(new PdfPCell(new Phrase("Department Average", headerFont)));

                // Данные
                foreach (var score in report.CriteriaScores)
                {
                    criteriaTable.AddCell(new PdfPCell(new Phrase(score.Criterion.Name, contentFont)));
                    criteriaTable.AddCell(new PdfPCell(new Phrase(score.AverageScore.ToString("F2"), contentFont)));
                    criteriaTable.AddCell(new PdfPCell(new Phrase(score.DepartmentAverage.ToString("F2"), contentFont)));
                }

                document.Add(criteriaTable);
            }
            else
            {
                document.Add(new Paragraph("No criteria evaluation data available for this period.", contentFont));
            }

            document.Add(new Paragraph(" ")); // Пустая строка

            // Посещаемость
            document.Add(new Paragraph("Attendance Statistics", sectionFont));
            document.Add(new Paragraph($"Total Work Days: {report.AttendanceStats.TotalWorkDays}", contentFont));
            document.Add(new Paragraph($"Days Present: {report.AttendanceStats.DaysPresent}", contentFont));
            document.Add(new Paragraph($"Days Absent: {report.AttendanceStats.DaysAbsent}", contentFont));
            document.Add(new Paragraph($"Vacation Days: {report.AttendanceStats.VacationDays}", contentFont));
            document.Add(new Paragraph($"Sick Leave Days: {report.AttendanceStats.SickDays}", contentFont));
            document.Add(new Paragraph($"Late Arrivals: {report.AttendanceStats.LateArrivals}", contentFont));

            // Добавление диаграмм и графиков здесь будет реализовано в будущем
        }

        /// <summary>
        /// Добавление содержимого отчета по отделу в PDF
        /// </summary>
        private async Task AddDepartmentReportContentPdf(Document document, DepartmentReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по критериям в PDF
        /// </summary>
        private async Task AddCriteriaAnalysisReportContentPdf(Document document, CriteriaAnalysisReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по динамике показателей в PDF
        /// </summary>
        private async Task AddPerformanceTrendReportContentPdf(Document document, PerformanceTrendReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по посещаемости в PDF
        /// </summary>
        private async Task AddAttendanceReportContentPdf(Document document, AttendanceReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого аналитического отчета в PDF
        /// </summary>
        private async Task AddAnalyticalReportContentPdf(Document document, AnalyticalReport report)
        {
            // Реализация будет добавлена позднее
        }

        #endregion

        #region Excel Export Methods

        /// <summary>
        /// Добавление содержимого индивидуального отчета в Excel
        /// </summary>
        private async Task AddEmployeeReportContentExcel(ExcelPackage package, EmployeeReport report)
        {
            // Основная информация
            var infoSheet = package.Workbook.Worksheets.Add("Employee Info");
            infoSheet.Cells[1, 1].Value = report.Title;
            infoSheet.Cells[1, 1, 1, 6].Merge = true;
            infoSheet.Cells[1, 1].Style.Font.Bold = true;
            infoSheet.Cells[1, 1].Style.Font.Size = 16;

            infoSheet.Cells[3, 1].Value = "Employee Information";
            infoSheet.Cells[3, 1].Style.Font.Bold = true;

            infoSheet.Cells[4, 1].Value = "Name:";
            infoSheet.Cells[4, 2].Value = report.Employee.FullName;

            infoSheet.Cells[5, 1].Value = "Position:";
            infoSheet.Cells[5, 2].Value = report.Employee.Position;

            infoSheet.Cells[6, 1].Value = "Department:";
            infoSheet.Cells[6, 2].Value = report.Employee.Department.Name;

            infoSheet.Cells[7, 1].Value = "Hire Date:";
            infoSheet.Cells[7, 2].Value = report.Employee.HireDate;
            infoSheet.Cells[7, 2].Style.Numberformat.Format = "dd.mm.yyyy";

            infoSheet.Cells[9, 1].Value = "Performance Summary";
            infoSheet.Cells[9, 1].Style.Font.Bold = true;

            infoSheet.Cells[10, 1].Value = "Average Score:";
            infoSheet.Cells[10, 2].Value = report.AverageScore;
            infoSheet.Cells[10, 2].Style.Numberformat.Format = "0.00";

            infoSheet.Cells[11, 1].Value = "Tasks Completed:";
            infoSheet.Cells[11, 2].Value = report.TaskStats.CompletedTasks;

            infoSheet.Cells[12, 1].Value = "Average Task Efficiency:";
            infoSheet.Cells[12, 2].Value = report.TaskStats.AverageEfficiency / 100;
            infoSheet.Cells[12, 2].Style.Numberformat.Format = "0.00%";

            infoSheet.Cells[13, 1].Value = "Attendance Rate:";
            infoSheet.Cells[13, 2].Value = report.AttendanceStats.AttendanceRate;
            infoSheet.Cells[13, 2].Style.Numberformat.Format = "0.00%";

            // Оценки
            var evalSheet = package.Workbook.Worksheets.Add("Evaluations");
            evalSheet.Cells[1, 1].Value = "Evaluation History";
            evalSheet.Cells[1, 1].Style.Font.Bold = true;
            evalSheet.Cells[1, 1].Style.Font.Size = 14;

            // Заголовки
            evalSheet.Cells[3, 1].Value = "Date";
            evalSheet.Cells[3, 2].Value = "Evaluator";
            evalSheet.Cells[3, 3].Value = "Score";
            evalSheet.Cells[3, 4].Value = "Notes";

            evalSheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;

            // Данные
            int row = 4;
            foreach (var eval in report.Evaluations)
            {
                evalSheet.Cells[row, 1].Value = eval.EvaluationDate;
                evalSheet.Cells[row, 1].Style.Numberformat.Format = "dd.mm.yyyy";
                evalSheet.Cells[row, 2].Value = eval.Evaluator.Username;
                evalSheet.Cells[row, 3].Value = eval.Score;
                evalSheet.Cells[row, 3].Style.Numberformat.Format = "0.00";
                evalSheet.Cells[row, 4].Value = eval.Notes;
                row++;
            }

            // Оценки по критериям
            var criteriaSheet = package.Workbook.Worksheets.Add("Criteria Scores");
            criteriaSheet.Cells[1, 1].Value = "Evaluation by Criteria";
            criteriaSheet.Cells[1, 1].Style.Font.Bold = true;
            criteriaSheet.Cells[1, 1].Style.Font.Size = 14;

            // Заголовки
            criteriaSheet.Cells[3, 1].Value = "Criterion";
            criteriaSheet.Cells[3, 2].Value = "Score";
            criteriaSheet.Cells[3, 3].Value = "Department Average";
            criteriaSheet.Cells[3, 4].Value = "Company Average";

            criteriaSheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;

            // Данные
            row = 4;
            foreach (var score in report.CriteriaScores)
            {
                criteriaSheet.Cells[row, 1].Value = score.Criterion.Name;
                criteriaSheet.Cells[row, 2].Value = score.AverageScore;
                criteriaSheet.Cells[row, 2].Style.Numberformat.Format = "0.00";
                criteriaSheet.Cells[row, 3].Value = score.DepartmentAverage;
                criteriaSheet.Cells[row, 3].Style.Numberformat.Format = "0.00";
                criteriaSheet.Cells[row, 4].Value = score.CompanyAverage;
                criteriaSheet.Cells[row, 4].Style.Numberformat.Format = "0.00";
                row++;
            }

            // Посещаемость
            var attendanceSheet = package.Workbook.Worksheets.Add("Attendance");
            attendanceSheet.Cells[1, 1].Value = "Attendance Statistics";
            attendanceSheet.Cells[1, 1].Style.Font.Bold = true;
            attendanceSheet.Cells[1, 1].Style.Font.Size = 14;

            attendanceSheet.Cells[3, 1].Value = "Total Work Days:";
            attendanceSheet.Cells[3, 2].Value = report.AttendanceStats.TotalWorkDays;

            attendanceSheet.Cells[4, 1].Value = "Days Present:";
            attendanceSheet.Cells[4, 2].Value = report.AttendanceStats.DaysPresent;

            attendanceSheet.Cells[5, 1].Value = "Days Absent:";
            attendanceSheet.Cells[5, 2].Value = report.AttendanceStats.DaysAbsent;

            attendanceSheet.Cells[6, 1].Value = "Vacation Days:";
            attendanceSheet.Cells[6, 2].Value = report.AttendanceStats.VacationDays;

            attendanceSheet.Cells[7, 1].Value = "Sick Leave Days:";
            attendanceSheet.Cells[7, 2].Value = report.AttendanceStats.SickDays;

            attendanceSheet.Cells[8, 1].Value = "Late Arrivals:";
            attendanceSheet.Cells[8, 2].Value = report.AttendanceStats.LateArrivals;

            attendanceSheet.Cells[9, 1].Value = "Attendance Rate:";
            attendanceSheet.Cells[9, 2].Value = report.AttendanceStats.AttendanceRate;
            attendanceSheet.Cells[9, 2].Style.Numberformat.Format = "0.00%";

            // Графики и диаграммы

            // Круговая диаграмма посещаемости
            if (report.AttendanceStats.TotalWorkDays > 0)
            {
                var chartSheet = package.Workbook.Worksheets.Add("Charts");
                chartSheet.Cells[5, 1].Value = "Vacation";
                chartSheet.Cells[5, 2].Value = report.AttendanceStats.VacationDays;

                chartSheet.Cells[6, 1].Value = "Sick Leave";
                chartSheet.Cells[6, 2].Value = report.AttendanceStats.SickDays;

                chartSheet.Cells[7, 1].Value = "Absent";
                chartSheet.Cells[7, 2].Value = report.AttendanceStats.DaysAbsent;

                // Создание круговой диаграммы
                var pieChart = chartSheet.Drawings.AddChart("AttendancePieChart", eChartType.Pie);
                pieChart.SetPosition(10, 0, 0, 0);
                pieChart.SetSize(500, 300);
                pieChart.Title.Text = "Attendance Distribution";
                pieChart.Series.Add(chartSheet.Cells["B4:B7"], chartSheet.Cells["A4:A7"]);
                pieChart.Legend.Position = eLegendPosition.Right;

                // Данные для графика эффективности выполнения задач
                if (report.TaskStats.EfficiencyTrend.Any())
                {
                    chartSheet.Cells[10, 1].Value = "Month";
                    chartSheet.Cells[10, 2].Value = "Tasks Completed";
                    chartSheet.Cells[10, 3].Value = "Efficiency";

                    row = 11;
                    foreach (var trend in report.TaskStats.EfficiencyTrend)
                    {
                        chartSheet.Cells[row, 1].Value = trend.Month;
                        chartSheet.Cells[row, 1].Style.Numberformat.Format = "mm/yyyy";
                        chartSheet.Cells[row, 2].Value = trend.CompletedTasks;
                        chartSheet.Cells[row, 3].Value = trend.AverageEfficiency / 100;
                        chartSheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";
                        row++;
                    }

                    // Создание линейного графика
                    var lineChart = chartSheet.Drawings.AddChart("EfficiencyLineChart", eChartType.Line);
                    lineChart.SetPosition(25, 0, 0, 0);
                    lineChart.SetSize(500, 300);
                    lineChart.Title.Text = "Task Efficiency Trend";
                    lineChart.XAxis.Title.Text = "Month";
                    lineChart.YAxis.Title.Text = "Efficiency";
                    lineChart.Series.Add(chartSheet.Cells[$"C11:C{row - 1}"], chartSheet.Cells[$"A11:A{row - 1}"]);
                    lineChart.Series[0].Header = "Efficiency";
                }
            }

            // Авто-подгонка ширины колонок для всех листов
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                worksheet.Cells.AutoFitColumns();
            }
        }

        /// <summary>
        /// Добавление содержимого отчета по отделу в Excel
        /// </summary>
        private async Task AddDepartmentReportContentExcel(ExcelPackage package, DepartmentReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по критериям в Excel
        /// </summary>
        private async Task AddCriteriaAnalysisReportContentExcel(ExcelPackage package, CriteriaAnalysisReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по динамике показателей в Excel
        /// </summary>
        private async Task AddPerformanceTrendReportContentExcel(ExcelPackage package, PerformanceTrendReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по посещаемости в Excel
        /// </summary>
        private async Task AddAttendanceReportContentExcel(ExcelPackage package, AttendanceReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого аналитического отчета в Excel
        /// </summary>
        private async Task AddAnalyticalReportContentExcel(ExcelPackage package, AnalyticalReport report)
        {
            // Реализация будет добавлена позднее
        }

        #endregion

        #region XML Export Methods

        /// <summary>
        /// Добавление содержимого индивидуального отчета в XML
        /// </summary>
        private async Task AddEmployeeReportContentXml(XmlWriter writer, EmployeeReport report)
        {
            // Информация о сотруднике
            writer.WriteStartElement("EmployeeInfo");

            writer.WriteElementString("Id", report.Employee.Id.ToString());
            writer.WriteElementString("FullName", report.Employee.FullName);
            writer.WriteElementString("Position", report.Employee.Position);
            writer.WriteElementString("Department", report.Employee.Department.Name);
            writer.WriteElementString("DepartmentId", report.Employee.DepartmentId.ToString());
            writer.WriteElementString("HireDate", report.Employee.HireDate.ToString("yyyy-MM-dd"));

            writer.WriteEndElement(); // EmployeeInfo

            // Сводная статистика
            writer.WriteStartElement("PerformanceSummary");

            writer.WriteElementString("AverageScore", report.AverageScore.ToString("F2"));
            writer.WriteElementString("CompletedTasks", report.TaskStats.CompletedTasks.ToString());
            writer.WriteElementString("AverageTaskEfficiency", report.TaskStats.AverageEfficiency.ToString("F2"));
            writer.WriteElementString("AttendanceRate", report.AttendanceStats.AttendanceRate.ToString("F4"));

            writer.WriteEndElement(); // PerformanceSummary

            // Оценки
            writer.WriteStartElement("Evaluations");

            foreach (var eval in report.Evaluations)
            {
                writer.WriteStartElement("Evaluation");

                writer.WriteElementString("Id", eval.Id.ToString());
                writer.WriteElementString("Date", eval.EvaluationDate.ToString("yyyy-MM-dd"));
                writer.WriteElementString("EvaluatorId", eval.EvaluatorId.ToString());
                writer.WriteElementString("EvaluatorName", eval.Evaluator.Username);
                writer.WriteElementString("Score", eval.Score.ToString("F2"));
                writer.WriteElementString("Notes", eval.Notes);

                writer.WriteEndElement(); // Evaluation
            }

            writer.WriteEndElement(); // Evaluations

            // Оценки по критериям
            writer.WriteStartElement("CriteriaScores");

            foreach (var score in report.CriteriaScores)
            {
                writer.WriteStartElement("CriterionScore");

                writer.WriteElementString("CriterionId", score.Criterion.Id.ToString());
                writer.WriteElementString("CriterionName", score.Criterion.Name);
                writer.WriteElementString("AverageScore", score.AverageScore.ToString("F2"));
                writer.WriteElementString("DepartmentAverage", score.DepartmentAverage.ToString("F2"));
                writer.WriteElementString("CompanyAverage", score.CompanyAverage.ToString("F2"));

                writer.WriteEndElement(); // CriterionScore
            }

            writer.WriteEndElement(); // CriteriaScores

            // Посещаемость
            writer.WriteStartElement("Attendance");

            writer.WriteElementString("TotalWorkDays", report.AttendanceStats.TotalWorkDays.ToString());
            writer.WriteElementString("DaysPresent", report.AttendanceStats.DaysPresent.ToString());
            writer.WriteElementString("DaysAbsent", report.AttendanceStats.DaysAbsent.ToString());
            writer.WriteElementString("VacationDays", report.AttendanceStats.VacationDays.ToString());
            writer.WriteElementString("SickDays", report.AttendanceStats.SickDays.ToString());
            writer.WriteElementString("LateArrivals", report.AttendanceStats.LateArrivals.ToString());
            writer.WriteElementString("AttendanceRate", report.AttendanceStats.AttendanceRate.ToString("F4"));

            writer.WriteEndElement(); // Attendance

            // Динамика эффективности задач
            writer.WriteStartElement("TaskEfficiencyTrend");

            foreach (var trend in report.TaskStats.EfficiencyTrend)
            {
                writer.WriteStartElement("Month");

                writer.WriteElementString("Date", trend.Month.ToString("yyyy-MM"));
                writer.WriteElementString("CompletedTasks", trend.CompletedTasks.ToString());
                writer.WriteElementString("AverageEfficiency", trend.AverageEfficiency.ToString("F2"));

                writer.WriteEndElement(); // Month
            }

            writer.WriteEndElement(); // TaskEfficiencyTrend
        }

        /// <summary>
        /// Добавление содержимого отчета по отделу в XML
        /// </summary>
        private async Task AddDepartmentReportContentXml(XmlWriter writer, DepartmentReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по критериям в XML
        /// </summary>
        private async Task AddCriteriaAnalysisReportContentXml(XmlWriter writer, CriteriaAnalysisReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по динамике показателей в XML
        /// </summary>
        private async Task AddPerformanceTrendReportContentXml(XmlWriter writer, PerformanceTrendReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого отчета по посещаемости в XML
        /// </summary>
        private async Task AddAttendanceReportContentXml(XmlWriter writer, AttendanceReport report)
        {
            // Реализация будет добавлена позднее
        }

        /// <summary>
        /// Добавление содержимого аналитического отчета в XML
        /// </summary>
        private async Task AddAnalyticalReportContentXml(XmlWriter writer, AnalyticalReport report)
        {
            // Реализация будет добавлена позднее
        }

        #endregion
    }
}
