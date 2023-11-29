using System;

namespace KayKitMultiplayerServer.Utility
{
    public class ValueTypeWrapper<T> where T : IComparable
    {
        public T Value { get; set; }

        public ValueTypeWrapper(T input)
        {
            Value = input;
        }

        public ValueTypeWrapper()
        {
        }

        public static implicit operator T(ValueTypeWrapper<T> input)
        {
            return input.Value;
        }

        public static implicit operator ValueTypeWrapper<T>(T input)
        {
            return new ValueTypeWrapper<T>(input);
        }

        public bool Equals(ValueTypeWrapper<T> x, ValueTypeWrapper<T> y)
        {
            return x.Value.Equals(y.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public int CompareTo(ValueTypeWrapper<T> other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(ValueTypeWrapper<T> other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            ValueTypeWrapper<T> other = obj as ValueTypeWrapper<T>;

            return other.Value.Equals(Value);
        }
    }
}