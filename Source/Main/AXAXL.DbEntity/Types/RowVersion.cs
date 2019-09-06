namespace System
{
	/// <summary>
	/// Custom value type for handling <see cref="System.Data.SqlDbType.Timestamp"/> which is a byte array. Although such Db type can be handled nicely by a regular c# byte array,
	/// serialization become tricky.  Having a custom value type would tie up custome handling nicely together.
	/// 
	/// Idea drawn from 
	/// 1. https://entityframework.net/knowledge-base/31163205/rowversion-gets-mapped-to-byte-8--in-entityframe-work-but-when-manually-casting-it-s-byte-18-
	/// 2. https://gist.github.com/jnm2/929d194c87df8ad0438f6cab0139a0a6
	/// </summary>
	public struct RowVersion : IComparable, IEquatable<RowVersion>, IComparable<RowVersion>, IConvertible
	{
		private readonly ulong internalValue;
		public static readonly RowVersion Empty = new RowVersion(0L);
		private RowVersion(ulong value)
		{
			this.internalValue = value;
		}
		public static implicit operator RowVersion(byte[] value)
		{
			return new RowVersion(ToUnsignedLong(value));
		}
		public static implicit operator RowVersion?(byte[] value)
		{
			if (value == null) return Empty;
			return new RowVersion(ToUnsignedLong(value));
		}
		public static implicit operator RowVersion(ulong value)
		{
			return new RowVersion(value);
		}
		public static implicit operator RowVersion(long value)
		{
			ulong unsignedLong = unchecked((ulong)value);

			return (RowVersion)unsignedLong;
		}
		public static implicit operator byte[](RowVersion value)
		{
			return ToByteArray(value.internalValue);
		}
		public static implicit operator ulong(RowVersion value)
		{
			return value.internalValue;
		}
		private static byte[] ToByteArray(ulong value)
		{
			var b = new byte[8];
			b[0] = (byte)(value >> 56);
			b[1] = (byte)(value >> 48);
			b[2] = (byte)(value >> 40);
			b[3] = (byte)(value >> 32);
			b[4] = (byte)(value >> 24);
			b[5] = (byte)(value >> 16);
			b[6] = (byte)(value >> 8);
			b[7] = (byte)(value);
			return b;
		}
		private static ulong ToUnsignedLong(byte[] bytes)
		{
			if (bytes == null) return 0L;
			if (bytes.Length != 8) throw new InvalidCastException("Cannot cast byte array with length other than 8.");

			ulong unsignedLong =
				((ulong)bytes[0] << 56) |
				((ulong)bytes[1] << 48) |
				((ulong)bytes[2] << 40) |
				((ulong)bytes[3] << 32) |
				((ulong)bytes[4] << 24) |
				((ulong)bytes[5] << 16) |
				((ulong)bytes[6] << 8) |
				((ulong)bytes[7]);
			return unsignedLong;
		}
		public static explicit operator long(RowVersion value)
		{
			return unchecked((long)value.internalValue);
		}
		public static bool operator ==(RowVersion leftOperand, RowVersion rightOperand)
		{
			return leftOperand.Equals(rightOperand);
		}
		public static bool operator !=(RowVersion leftOperand, RowVersion rightOperand)
		{
			return ! leftOperand.Equals(rightOperand);
		}
		public static bool operator >(RowVersion leftOperand, RowVersion rightOperand)
		{
			return leftOperand.CompareTo(rightOperand) == 1;
		}
		public static bool operator <(RowVersion leftOperand, RowVersion rightOperand)
		{
			return leftOperand.CompareTo(rightOperand) == -1;
		}
		public static bool operator >=(RowVersion leftOperand, RowVersion rightOperand)
		{
			return leftOperand.CompareTo(rightOperand) >= 0;
		}
		public static bool operator <=(RowVersion leftOperand, RowVersion rightOperand)
		{
			return leftOperand.CompareTo(rightOperand) <= 0;
		}
		public bool Equals(RowVersion rowVersion)
		{
			return rowVersion.internalValue == this.internalValue;
		}
		public int CompareTo(RowVersion theOther)
		{
			return this.internalValue == theOther.internalValue ? 0 : (this.internalValue < theOther.internalValue ? -1 : 1);
		}
		public int CompareTo(object obj)
		{
			return obj == null ? 1 : this.CompareTo((RowVersion)obj);
		}
		public override int GetHashCode()
		{
			return this.internalValue.GetHashCode();
		}
		public override string ToString() 
		{
			return String.Format("0x{0:X16}", this.internalValue);
		}
		public override bool Equals(object obj)
		{
			return this.CompareTo(obj) == 0;
		}
		public TypeCode GetTypeCode()
		{
			return TypeCode.Object;
		}

		public bool ToBoolean(IFormatProvider provider)
		{
			return this != Empty;
		}

		public byte ToByte(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToByte)} is not implemented yet");
		}

		public char ToChar(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToChar)} is not implemented yet");
		}

		public DateTime ToDateTime(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToDateTime)} is not implemented yet");
		}

		public decimal ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this.internalValue);
		}

		public double ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this.internalValue);
		}

		public short ToInt16(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToInt16)} is not implemented yet");
		}

		public int ToInt32(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToInt32)} is not implemented yet");
		}

		public long ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this.internalValue);
		}

		public sbyte ToSByte(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToSByte)} is not implemented yet");
		}

		public float ToSingle(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToSingle)} is not implemented yet");
		}

		public string ToString(IFormatProvider provider)
		{
			return this.ToString();
		}

		public object ToType(Type conversionType, IFormatProvider provider)
		{
			var typeCode = Type.GetTypeCode(conversionType);
			switch(typeCode)
			{
				case TypeCode.Boolean:
					return this.ToBoolean(provider);
				case TypeCode.Byte:
					return this.ToByte(provider);
				case TypeCode.Char:
					return this.ToChar(provider);
				case TypeCode.DBNull:
					return default(RowVersion);
				case TypeCode.DateTime:
					return this.ToDateTime(provider);
				case TypeCode.Decimal:
					return this.ToDecimal(provider);
				case TypeCode.Double:
					return this.ToDouble(provider);
				case TypeCode.Empty:
					return Empty;
				case TypeCode.Int16:
					return this.ToInt16(provider);
				case TypeCode.Int32:
					return this.ToInt32(provider);
				case TypeCode.Int64:
					return this.ToUInt64(provider);
				case TypeCode.Object:
					if (conversionType.IsAssignableFrom(typeof(byte[])))
					{
						return ToByteArray(this.internalValue);
					}
					else
					{
						throw new InvalidCastException($"Failed to cast frm RowVersion to type {conversionType.FullName}");
					}
				case TypeCode.SByte:
					return this.ToSByte(provider);
				case TypeCode.Single:
					return this.ToSingle(provider);
				case TypeCode.String:
					return this.ToString(provider);
				case TypeCode.UInt16:
					return this.ToUInt16(provider);
				case TypeCode.UInt32:
					return this.ToUInt32(provider);
				case TypeCode.UInt64:
					return this.ToUInt64(provider);
				default:
					throw new InvalidOperationException($"Encountered unexpected TypeCode {typeCode}");
			}
		}

		public ushort ToUInt16(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToUInt16)} is not implemented yet");
		}

		public uint ToUInt32(IFormatProvider provider)
		{
			throw new NotImplementedException($"{nameof(ToUInt32)} is not implemented yet");
		}

		public ulong ToUInt64(IFormatProvider provider)
		{
			return this.internalValue;
		}
	}
}