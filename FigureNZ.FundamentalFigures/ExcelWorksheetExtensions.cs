using System;
using System.Collections.Generic;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FigureNZ.FundamentalFigures
{
    public static class ExcelWorksheetExtensions
    {
        public static ExcelWorksheet FromRecords(this ExcelWorksheet worksheet, List<Record> set)
        {
            int row = 1;
            string discriminatorLabel = null;
            string measureLabel = null;

            foreach (Record record in set)
            {
                int col = 1;

                if (measureLabel != record.MeasureFormatted() || discriminatorLabel != record.Discriminator)
                {
                    discriminatorLabel = record.Discriminator;
                    worksheet.Cells[row, col].Value = discriminatorLabel;
                    col++;

                    measureLabel = record.MeasureFormatted();
                    worksheet.Cells[row, col].Value = measureLabel;
                    col++;
                }
                else
                {
                    col++;
                    col++;
                }

                worksheet.Cells[row, col].Value = record.CategoryFormatted();
                col++;

                if (record.Value == null)
                {
                    record.ValueUnit = "null";
                }

                switch (record.ValueUnit)
                {
                    case "null":
                        worksheet.Cells[row, col].Value = record.NullReason;
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        break;

                    case "nzd":
                        worksheet.Cells[row, col].Value = record.Value;
                        worksheet.Cells[row, col].Style.Numberformat.Format = "$###,###,###,###,###,###,###,###,##0.00";
                        break;

                    case "percentage":
                        worksheet.Cells[row, col].Value = record.Value / 100;
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00%";
                        break;

                    case "number":
                    default:
                        worksheet.Cells[row, col].Value = record.Value;

                        if (record.Value % 1 != 0)
                        {
                            // This number has decimal places, so format expecting a decimal point
                            worksheet.Cells[row, col].Style.Numberformat.Format = "###,###,###,###,###,###,###,###,##0.##";
                        }
                        else
                        {
                            // This number is a whole number, with no decimal point
                            worksheet.Cells[row, col].Style.Numberformat.Format = "###,###,###,###,###,###,###,###,##0";
                        }

                        break;
                }
                col++;

                worksheet.Cells[row, col].Value = record.ValueLabel;
                col++;

                worksheet.Cells[row, col].Value = record.Date;
                col++;

                worksheet.Cells[row, col].Value = record.DateLabel;
                col++;

                worksheet.Cells[row, col].Value = record.Uri.ToString().Replace("/download", string.Empty, StringComparison.OrdinalIgnoreCase);
                col++;

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return worksheet;
        }
    }
}
