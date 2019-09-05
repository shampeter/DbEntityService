using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	/// <summary>
	/// Copied from https://gist.githubusercontent.com/jnm2/929d194c87df8ad0438f6cab0139a0a6/raw/8a9e2f0628684494f078c58d5bb43d1e8cc1f671/Timestamp.cs
	/// As discovered from discussion https://stackoverflow.com/questions/31163205/rowversion-gets-mapped-to-byte8-in-entityframe-work-but-when-manually-casting
	/// </summary>
	//[DebuggerDisplay("{ToString(),nq}")]
	public struct Timestamp : IComparable, IEquatable<Timestamp>, IComparable<Timestamp>
	{
		public static readonly Timestamp Zero = default(Timestamp);

		private readonly ulong value;

		private Timestamp(ulong value)
		{
			this.value = value;
		}

		public static implicit operator Timestamp(ulong value)
		{
			return new Timestamp(value);
		}
		public static implicit operator Timestamp(long value)
		{
			return new Timestamp(unchecked((ulong)value));
		}
		public static explicit operator Timestamp(byte[] value)
		{
			return new Timestamp(((ulong)value[0] << 56) | ((ulong)value[1] << 48) | ((ulong)value[2] << 40) | ((ulong)value[3] << 32) | ((ulong)value[4] << 24) | ((ulong)value[5] << 16) | ((ulong)value[6] << 8) | value[7]);
		}
		public static explicit operator Timestamp?(byte[] value)
		{
			if (value == null) return null;
			return new Timestamp(((ulong)value[0] << 56) | ((ulong)value[1] << 48) | ((ulong)value[2] << 40) | ((ulong)value[3] << 32) | ((ulong)value[4] << 24) | ((ulong)value[5] << 16) | ((ulong)value[6] << 8) | value[7]);
		}
		public static implicit operator byte[](Timestamp timestamp)
		{
			var r = new byte[8];
			r[0] = (byte)(timestamp.value >> 56);
			r[1] = (byte)(timestamp.value >> 48);
			r[2] = (byte)(timestamp.value >> 40);
			r[3] = (byte)(timestamp.value >> 32);
			r[4] = (byte)(timestamp.value >> 24);
			r[5] = (byte)(timestamp.value >> 16);
			r[6] = (byte)(timestamp.value >> 8);
			r[7] = (byte)timestamp.value;
			return r;
		}
		public static implicit operator ulong(Timestamp timestamp)
		{
			return timestamp.value;
		}
		public static implicit operator long(Timestamp timestamp)
		{
			return unchecked((long)timestamp.value);
		}
		public override bool Equals(object obj)
		{
			return obj is Timestamp && Equals((Timestamp)obj);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public bool Equals(Timestamp other)
		{
			return other.value == value;
		}

		int IComparable.CompareTo(object obj)
		{
			return obj == null ? 1 : CompareTo((Timestamp)obj);
		}

		public int CompareTo(Timestamp other)
		{
			return value == other.value ? 0 : value < other.value ? -1 : 1;
		}

		public static bool operator ==(Timestamp comparand1, Timestamp comparand2)
		{
			return comparand1.Equals(comparand2);
		}
		public static bool operator !=(Timestamp comparand1, Timestamp comparand2)
		{
			return !comparand1.Equals(comparand2);
		}
		public static bool operator >(Timestamp comparand1, Timestamp comparand2)
		{
			return comparand1.CompareTo(comparand2) > 0;
		}
		public static bool operator >=(Timestamp comparand1, Timestamp comparand2)
		{
			return comparand1.CompareTo(comparand2) >= 0;
		}
		public static bool operator <(Timestamp comparand1, Timestamp comparand2)
		{
			return comparand1.CompareTo(comparand2) < 0;
		}
		public static bool operator <=(Timestamp comparand1, Timestamp comparand2)
		{
			return comparand1.CompareTo(comparand2) <= 0;
		}

		public override string ToString()
		{
			return value.ToString("x16");
		}

		public static Timestamp Max(Timestamp comparand1, Timestamp comparand2)
		{
			return comparand1.value < comparand2.value ? comparand2 : comparand1;
		}
	}
}
