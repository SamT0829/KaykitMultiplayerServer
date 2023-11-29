using System.Collections.Generic;
using System;
using System.Threading;
using KayKitMultiplayerServer.Utility;

namespace KayKitMultiplayerServer.TableRelated
{
    public abstract class TableBase
    {
        private const int _typeRowNumber = 0;
        private const int _headerRowNumber = 1;
        private const int _contentStartRowNumber = 2;
        private const int _firstRowNumber = 0;
        private const int _invalidRowNumber = -1;
        private const int _secondIndexNumber = 1;
        private const string _skipFlag = "#";

        private const string _typeInt = "int";
        private const string _typeShort = "short";
        private const string _typeByte = "byte";
        private const string _typeString = "string";
        private const string _typeFloat = "float";
        private const string _typeLong = "long";

        private Dictionary<string, int> _columnNameToIndex;
        private Dictionary<int, string> _columnIndexToName;
        private List<Type> _columnTypeList = new List<Type>();
        private Dictionary<object, List<object>> _content = new Dictionary<object, List<object>>();
        private Dictionary<Type, Func<string, object>> _transformTable = new Dictionary<Type, Func<string, object>>();

        protected ReaderWriterLock _lock = new ReaderWriterLock();

        public string[] LineSeparator { set; get; }
        public string[] ColumnSeparator { set; get; }

        protected TableBase()
            : this(new string[] { "\n" }, new string[] { "\t" })
        {
        }

        protected TableBase(string[] lineSeparator, string[] columnSeparator)
        {
            LineSeparator = lineSeparator;
            ColumnSeparator = columnSeparator;
            _transformTable.Add(typeof(int), GetIntObject);
            _transformTable.Add(typeof(short), GetShortObject);
            _transformTable.Add(typeof(byte), GetByteObject);
            _transformTable.Add(typeof(string), GetStringObject);
            _transformTable.Add(typeof(float), GetFloatObject);
            _transformTable.Add(typeof(long), GetLongObject);
        }

        public void Parsing(string wholeText)
        {
            //DebugLog.Log("start parse Table : " + GetType().ToString());
            string[] lines = wholeText.Split(LineSeparator, StringSplitOptions.None);
            ParseColumnName(lines[_headerRowNumber], out _columnNameToIndex, out _columnIndexToName);
            ParseColumnType(lines[_typeRowNumber], out _columnTypeList);

            for (int i = _contentStartRowNumber; i < lines.Length - 1; ++i)
            {
                List<object> rowContent;
                if (lines[i].StartsWith(_skipFlag))
                {
                    continue;
                }
                object index = ParseRow(lines[i], out rowContent);
                _lock.AcquireWriterLock(1000);
                if (!_content.ContainsKey(index))
                {
                    _content.Add(index, rowContent);
                }
                _lock.ReleaseWriterLock();
                OnRowParsed(rowContent);
            }
            OnTableParsed();
        }

        public List<object> GetRows<T>(ValueTypeWrapper<T> index)
            where T : IComparable
        {
            List<object> rows;
            _lock.AcquireReaderLock(1000);
            _content.TryGetValue(index, out rows);
            _lock.ReleaseReaderLock();
            return rows;
        }

        public object GetValue<Tkey>(ValueTypeWrapper<Tkey> index, string columnName)
            where Tkey : IComparable
        {
            List<object> rows = GetRows(index);
            int columnIndex = GetColumnNameIndex(columnName);
            if (rows.Count > columnIndex)
            {
                _lock.AcquireReaderLock(1000);
                object retv = rows[columnIndex];
                _lock.ReleaseReaderLock();
                return retv;
            }
            return null;
        }

        public Toutput GetValue<Tkey, Toutput>(ValueTypeWrapper<Tkey> index, string columnName)
            where Tkey : IComparable
            where Toutput : IComparable
        {
            List<object> rows = GetRows(index);
            int columnIndex = GetColumnNameIndex(columnName);
            if (rows.Count > columnIndex)
            {
                _lock.AcquireReaderLock(1000);
                ValueTypeWrapper<Toutput> output = rows[columnIndex] as ValueTypeWrapper<Toutput>;
                _lock.ReleaseReaderLock();
                if (output != null)
                {
                    return output.Value;
                }
            }
            return default(Toutput);
        }

        public int GetColumnNameIndex(string columnName)
        {
            int index;
            _lock.AcquireReaderLock(1000);
            if (!_columnNameToIndex.TryGetValue(columnName, out index))
            {
                index = _invalidRowNumber;
            }
            _lock.ReleaseReaderLock();
            return index;
        }
        public string GetColumnName(int index)
        {
            string name;
            _lock.AcquireReaderLock(1000);
            if (!_columnIndexToName.TryGetValue(index, out name))
            {
                name = string.Empty;
            }
            _lock.ReleaseReaderLock();
            return name;
        }

        public Type GetColumnType(int columnIndex)
        {
            _lock.AcquireReaderLock(1000);
            Type retv = _columnTypeList[columnIndex];
            _lock.ReleaseReaderLock();
            return retv;
        }

        protected abstract void OnRowParsed(List<object> rowContent);
        protected abstract void OnTableParsed();

        private void ParseColumnName(string columnRow, out Dictionary<string, int> columnNameTable, out Dictionary<int, string> columnIndexTable)
        {
            columnNameTable = new Dictionary<string, int>();
            columnIndexTable = new Dictionary<int, string>();
            string[] headers = columnRow.Split(ColumnSeparator, StringSplitOptions.None);
            _lock.AcquireWriterLock(1000);
            for (int i = 0; i < headers.Length; ++i)
            {
                columnNameTable.Add(headers[i], i);
                columnIndexTable.Add(i, headers[i]);
            }
            _lock.ReleaseWriterLock();
        }

        private void ParseColumnType(string columnRow, out List<Type> rowFields)
        {
            string[] types = columnRow.Split(ColumnSeparator, StringSplitOptions.None);
            rowFields = new List<Type>(types.Length);
            for (int i = 0; i < types.Length; ++i)
            {
                switch (types[i])
                {
                    case _typeInt:
                        {
                            rowFields.Add(typeof(int));
                        }
                        break;
                    case _typeShort:
                        {
                            rowFields.Add(typeof(short));
                        }
                        break;
                    case _typeByte:
                        {
                            rowFields.Add(typeof(byte));
                        }
                        break;
                    case _typeString:
                        {
                            rowFields.Add(typeof(string));
                        }
                        break;
                    case _typeFloat:
                        {
                            rowFields.Add(typeof(float));
                        }
                        break;
                    case _typeLong:
                        {
                            rowFields.Add(typeof(long));
                        }
                        break;
                }
            }
        }

        private object ParseRow(string columnRow, out List<object> rowFields)
        {
            string[] items = columnRow.Split(ColumnSeparator, StringSplitOptions.None);
            if (items.Length <= 1)
            {
                rowFields = null;
                return null;
            }
            rowFields = new List<object>(items.Length);
            for (int i = 0; i < items.Length - 1; ++i)
            {
                Type t = GetColumnType(i);
                _lock.AcquireReaderLock(1000);
                if (_transformTable.ContainsKey(t))
                {
                    rowFields.Add(_transformTable[t](items[i]));
                }
                _lock.ReleaseReaderLock();
            }
            return rowFields[_firstRowNumber];
        }

        private ValueTypeWrapper<int> GetIntObject(string inputString)
        {
            ValueTypeWrapper<int> retValue = int.Parse(inputString);
            return retValue;
        }

        private ValueTypeWrapper<short> GetShortObject(string inputString)
        {
            ValueTypeWrapper<short> retValue = short.Parse(inputString);
            return retValue;
        }

        private ValueTypeWrapper<byte> GetByteObject(string inputString)
        {
            ValueTypeWrapper<byte> retValue = byte.Parse(inputString);
            return retValue;
        }

        private ValueTypeWrapper<string> GetStringObject(string inputString)
        {
            ValueTypeWrapper<string> retValue = inputString;
            return retValue;
        }

        private ValueTypeWrapper<float> GetFloatObject(string inputString)
        {
            ValueTypeWrapper<float> retValue = float.Parse(inputString);
            return retValue;
        }

        private ValueTypeWrapper<long> GetLongObject(string inputString)
        {
            ValueTypeWrapper<long> retValue = long.Parse(inputString);
            return retValue;
        }
    }
}