using KayKitMultiplayerServer.Utility;
using System.Collections.Generic;
using System;

namespace KayKitMultiplayerServer.TableRelated
{
    public abstract class CompositeKeyTable : TableBase
    {
        protected int _keyOneIndex;
        protected int _keyTwoIndex;

        private const int _defaultKey1 = 0;
        private const int _defaultKey2 = 1;

        private Dictionary<object, Dictionary<object, List<object>>> _compositeKeyTable =
            new Dictionary<object, Dictionary<object, List<object>>>();

        protected CompositeKeyTable()
            : this(_defaultKey1, _defaultKey2)
        {

        }

        protected CompositeKeyTable(int keyOneIndex, int keyTwoIndex)
            : this(keyOneIndex, keyTwoIndex, new string[] { "\n" }, new string[] { "\t" })
        {

        }

        protected CompositeKeyTable(string[] lineSeparator, string[] columnSeparator)
            : this(_defaultKey1, _defaultKey2, lineSeparator, columnSeparator)
        {
        }

        protected CompositeKeyTable(int keyOneIndex, int keyTwoIndex, string[] lineSeparator, string[] columnSeparator)
            : base(lineSeparator, columnSeparator)
        {
            _keyOneIndex = keyOneIndex;
            _keyTwoIndex = keyTwoIndex;
        }

        public Toutput GetValue<Tkey1, Tkey2, Toutput>(ValueTypeWrapper<Tkey1> key1, ValueTypeWrapper<Tkey2> key2, string columnName)
            where Tkey1 : IComparable
            where Tkey2 : IComparable
            where Toutput : IComparable
        {
            _lock.AcquireReaderLock(1000);
            Dictionary<object, List<object>> subTable;
            if (!_compositeKeyTable.TryGetValue(key1, out subTable) || subTable == null)
            {
                _lock.ReleaseReaderLock();
                return default(Toutput);
            }
            List<object> row;
            if (!subTable.TryGetValue(key2, out row) || row == null)
            {
                _lock.ReleaseReaderLock();
                return default(Toutput);
            }

            int columnIndex = GetColumnNameIndex(columnName);
            if (row.Count > columnIndex)
            {
                ValueTypeWrapper<Toutput> output = row[columnIndex] as ValueTypeWrapper<Toutput>;
                if (output != null)
                {
                    _lock.ReleaseReaderLock();
                    return output.Value;
                }
            }
            _lock.ReleaseReaderLock();
            return default(Toutput);
        }

        public List<object> GetRows<Tkey1, Tkey2>(ValueTypeWrapper<Tkey1> key1, ValueTypeWrapper<Tkey2> key2)
            where Tkey1 : IComparable
            where Tkey2 : IComparable
        {
            _lock.AcquireReaderLock(1000);
            Dictionary<object, List<object>> subTable;
            if (!_compositeKeyTable.TryGetValue(key1, out subTable) || subTable == null)
            {
                _lock.ReleaseReaderLock();
                return null;
            }
            List<object> row;
            if (!subTable.TryGetValue(key2, out row) || row == null)
            {
                _lock.ReleaseReaderLock();
                return null;
            }
            _lock.ReleaseReaderLock();
            return row;
        }

        public Dictionary<object, List<object>> GetTable<Tkey1>(ValueTypeWrapper<Tkey1> key1)
            where Tkey1 : IComparable
        {
            _lock.AcquireReaderLock(1000);
            Dictionary<object, List<object>> subTable;
            if (!_compositeKeyTable.TryGetValue(key1, out subTable) || subTable == null)
            {
                _lock.ReleaseReaderLock();
                return null;
            }
            return subTable;
        }

        protected sealed override void OnRowParsed(List<object> rowContent)
        {
            if (rowContent.Count <= _keyOneIndex || rowContent.Count <= _keyTwoIndex)
            {
                return;
            }
            object keyOneObj = rowContent[_keyOneIndex];
            object keyTwoObj = rowContent[_keyTwoIndex];

            Dictionary<object, List<object>> subTable;
            if (!_compositeKeyTable.TryGetValue(keyOneObj, out subTable) || subTable == null)
            {
                subTable = new Dictionary<object, List<object>>();
                _compositeKeyTable.Add(keyOneObj, subTable);
            }
            if (!subTable.ContainsKey(keyTwoObj))
            {
                subTable.Add(keyTwoObj, rowContent);
            }

            OnCompositeKeyDealed(rowContent);
        }

        protected abstract void OnCompositeKeyDealed(List<object> rowContent);
        protected abstract override void OnTableParsed();
    }
}