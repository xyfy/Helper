using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Xyfy.Helper
{

    /// <summary>
    /// NPOI泛型帮助类
    /// </summary>
    public class NPOIHelper<T> : IDisposable where T : class, new()
    {
        /// <summary>
        /// Excel版本枚举
        /// </summary>
        public enum ExcelVersion
        {
            /// <summary>
            /// 2003|below<![CDATA[&below]]>
            /// </summary>
            xls,
            /// <summary>
            /// 2003up
            /// </summary>
            xlsx
        }
        private IWorkbook workbook;
        private string sheetName;
        private ISheet sheet;
        /// <summary>
        /// 文件路径
        /// </summary>
        private readonly string filePath;
        private readonly string[] propertyNames;
        private readonly int[] maxWidths;
        private readonly ExcelVersion version;
        private int currentRowIndex = 0;

        /// <summary>
        /// Excel版本
        /// </summary>
        public ExcelVersion Version
        {
            get
            {
                return version;
            }
        }
        /// <summary>
        /// 表格允许的最大列宽
        /// </summary>
        private const int MAX_COLUMN_WIDTH = 100;
        /// <summary>
        /// xlsx允许的最大行数
        /// </summary>
        public const int MAX_ALLOW_ROW_NUMBER_XLSX = 1048576;
        /// <summary>
        /// xls允许的最大行数
        /// </summary>
        public const int MAX_ALLOW_ROW_NUMBER_XLS = 65535;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filePath">物理文件路径, 只支持xls和xlsx格式</param>
        /// <param name="propertyNames">需要导出的字段名称</param>
        /// <param name="headers">标题名</param>
        ///<param name="sheetName"></param>
        public NPOIHelper(string filePath, string[] propertyNames, string[] headers = null, string sheetName = "sheet1")
        {
            this.filePath = filePath;
            FileInfo fi = new FileInfo(filePath);
            switch (fi.Extension.ToLower())
            {
                case ".xls":
                    version = ExcelVersion.xls;
                    break;
                case ".xlsx":
                    version = ExcelVersion.xlsx;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("filePath", "只支持excel文件导出!文件扩展名仅允许xls和xlsx");
            }
            this.propertyNames = propertyNames;
            this.sheetName = sheetName;
            maxWidths = new int[propertyNames.Length];
            if (headers != null)
            {
                WriteHeader(headers);
                currentRowIndex++;
            }
            setInfo();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="propertyNames">需要导出的字段名称</param>
        /// <param name="headers">标题名</param>
        ///<param name="sheetName">sheet名</param>
        /// <param name="version">需要导出的excel版本</param>
        public NPOIHelper(string[] propertyNames, string[] headers = null, string sheetName = "sheet1", ExcelVersion version = ExcelVersion.xlsx)
        {
            this.version = version;
            this.propertyNames = propertyNames;
            this.sheetName = sheetName;
            maxWidths = new int[propertyNames.Length];
            if (headers != null)
            {
                WriteHeader(headers);
                currentRowIndex++;
            }
            setInfo();
        }

        #region 私有方法
        /// <summary>
        /// 直接写入内存并返回字节
        /// </summary>
        /// <returns></returns>
        private byte[] WriteToMemory()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                ms.Flush();
                ms.Position = 0;
                return ms.GetBuffer();
            }
        }
        /// <summary>
        /// 设置变量
        /// </summary>
        private void setInfo()
        {
            switch (version)
            {
                case ExcelVersion.xls:
                    workbook = new HSSFWorkbook();
                    break;
                case ExcelVersion.xlsx:
                    workbook = new XSSFWorkbook();
                    break;
            }
            sheet = workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                sheet = workbook.CreateSheet(sheetName);
            }
        }

        /// <summary>
        /// 获取指定行,不存在则创建
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        private IRow GetRow(int rowIndex)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null)
            {
                row = sheet.CreateRow(rowIndex);
            }
            return row;
        }
        /// <summary>
        /// 获取指定单元格
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        private ICell GetCell(int rowIndex, int colIndex)
        {
            var row = GetRow(rowIndex);
            var cell = row.GetCell(colIndex);
            if (cell == null)
            {
                cell = row.CreateCell(colIndex);
                cell.CellStyle.WrapText = true;//自动换行
            }
            return cell;
        }
        /// <summary>
        /// 列头写入
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private bool WriteHeader(string[] headers)
        {
            var row = GetRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                GetCell(0, i).SetCellValue(header);
            }
            return true;
        }

        #endregion
        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="filePath"></param>
        public void WriteToFile(string filePath)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                workbook.Write(fs);
                fs.Flush();
            }
            catch
            {
            }
            finally
            {
                fs?.Close();
                Dispose();
            }
        }

        /// <summary>
        /// 行数据写入
        /// </summary>
        /// <param name="data"></param>
        /// <param name="beginRow"></param>
        /// <returns>数据写入完成返回true,数据空返回false</returns>
        public bool WriteData(List<T> data, int beginRow = 0)
        {
            if (beginRow > currentRowIndex)
            {
                //如果传入行大于当前行数,则把当前行换为新行数
                currentRowIndex = beginRow;
            }
            if (data.Count == 0) return false;
            //#region 批量移动行,由于是追加,不需要移动行
            //sheet.ShiftRows(beginRow, sheet.LastRowNum, data.Count, true, false);
            //#endregion
            foreach (var item in data)
            {
                PropertyInfo[] propertys = item.GetType().GetProperties();
                var cellIndex = 0;
                for (int j = 0; j < propertyNames.Length; j++)
                {
                    var nowProperty = propertyNames[j];
                    var val = string.Empty;
                    var pi = item.GetType().GetProperty(nowProperty, BindingFlags.Public | BindingFlags.Instance);
                    if (pi != null)
                    {
                        Type pt = pi.PropertyType;
                        //对于泛型和可空类型的特殊处理
                        if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            pt = pt.GetGenericArguments()[0];
                        }
                        object obj = pi.GetValue(item, null);
                        if (obj != null)
                        {
                            if (pt == typeof(DateTime))
                            {
                                val = ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            if (pt == typeof(double))
                            {
                                val = ((double)obj).ToString("N2");
                            }
                            if (pt == typeof(decimal))
                            {
                                val = ((double)obj).ToString("N2");
                            }
                            else
                            {
                                val = obj.ToString();
                            }
                        }
                    }
                    int length = Encoding.UTF8.GetBytes(val).Length;//获取当前单元格的内容宽度
                    if (maxWidths[cellIndex] < length + 1)
                    {
                        maxWidths[cellIndex] = length + 1;
                    }//若当前单元格内容宽度大于列宽，则调整列宽为当前单元格宽度，后面的+1是我人为的将宽度增加一个字符
                    var icell = GetCell(currentRowIndex, cellIndex);
                    icell.SetCellValue(val);
                    cellIndex++;
                }
                currentRowIndex++;//遍历完成后currentRowIndex+1;
            }
            //所有行遍历完成后,设置列宽
            for (int i = 0; i < maxWidths.Length; i++)
            {
                var width = maxWidths[i];
                width = width > MAX_COLUMN_WIDTH ? MAX_COLUMN_WIDTH : width;
                sheet.SetColumnWidth(i, width * 256);
            }
            return true;
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (workbook != null)
                {
                    workbook.Close();
                    workbook.Dispose();
                }
                //删除临时文件
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch
            {

            }
        }
    }
}