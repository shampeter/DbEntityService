using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ExpressionToString;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.MSSql
{
	internal partial class MSSqlGenerator : IMSSqlGenerator
	{
		#region Debug only code with Conditional defined.
		[Conditional("DEBUG")]
		private void LogDataFetchingExpression(string message, Expression expr)
		{
			this.log.LogDebug("{0} ... {1}", message, expr.ToString("C#"));
		}

		[Conditional("DEBUG")]
		private void LogSqlStatement(string sqlType, string sqlStatement)
		{
			this.log.LogDebug("{0}: {1}", sqlType, sqlStatement);
		}
		#endregion

		#region Sql Type mapping and Data Reader delegates.
		internal static Expression<Func<SqlDataReader, int, Int64>> _getSqlInt64 = (reader, idx) => reader.GetSqlInt64(idx).Value;
		internal static Expression<Func<SqlDataReader, int, byte[]>> _getSqlBinary = (reader, idx) => reader.GetSqlBinary(idx).Value;
		internal static Expression<Func<SqlDataReader, int, bool>> _getSqlBoolean = (reader, idx) => reader.GetSqlBoolean(idx).Value;
		internal static Expression<Func<SqlDataReader, int, DateTime>> _getSqlDateTime = (reader, idx) => reader.GetSqlDateTime(idx).Value;
		internal static Expression<Func<SqlDataReader, int, DateTimeOffset>> _getDateTimeOffset = (reader, idx) => reader.GetDateTimeOffset(idx);
		internal static Expression<Func<SqlDataReader, int, decimal>> _getSqlDecimal = (reader, idx) => reader.GetSqlDecimal(idx).Value;
		internal static Expression<Func<SqlDataReader, int, double>> _getSqlDouble = (reader, idx) => reader.GetSqlDouble(idx).Value;
		internal static Expression<Func<SqlDataReader, int, Int32>> _getSqlInt32 = (reader, idx) => reader.GetSqlInt32(idx).Value;
		internal static Expression<Func<SqlDataReader, int, float>> _getSqlSingle = (reader, idx) => reader.GetSqlSingle(idx).Value;
		internal static Expression<Func<SqlDataReader, int, Int16>> _getSqlInt16 = (reader, idx) => reader.GetSqlInt16(idx).Value;
		internal static Expression<Func<SqlDataReader, int, byte>> _getSqlByte = (reader, idx) => reader.GetSqlByte(idx).Value;
		internal static Expression<Func<SqlDataReader, int, Guid>> _getSqlGuid = (reader, idx) => reader.GetSqlGuid(idx).Value;
		internal static Expression<Func<SqlDataReader, int, string>> _getSqlString = (reader, idx) => reader.GetSqlString(idx).Value;
		internal static Expression<Func<SqlDataReader, int, decimal>> _getSqlMoney = (reader, idx) => reader.GetSqlMoney(idx).Value;
		internal static Dictionary<Type, SqlDbType> CSTypeToSqlTypeMap = new Dictionary<Type, SqlDbType>
		{
			[typeof(Int64)] = SqlDbType.BigInt,
			[typeof(Byte[])] = SqlDbType.VarBinary,
			[typeof(Boolean)] = SqlDbType.Bit,
			[typeof(DateTime)] = SqlDbType.DateTime,
			[typeof(DateTimeOffset)] = SqlDbType.DateTimeOffset,
			[typeof(Decimal)] = SqlDbType.Decimal,
			[typeof(Double)] = SqlDbType.Float,
			[typeof(Int32)] = SqlDbType.Int,
			[typeof(Single)] = SqlDbType.Real,
			[typeof(Int16)] = SqlDbType.SmallInt,
			[typeof(Byte)] = SqlDbType.TinyInt,
			[typeof(Guid)] = SqlDbType.UniqueIdentifier,
			[typeof(String)] = SqlDbType.VarChar,
			[typeof(RowVersion)] = SqlDbType.VarBinary,
			// Nullables
			[typeof(Int64?)] = SqlDbType.BigInt,
			[typeof(Boolean?)] = SqlDbType.Bit,
			[typeof(DateTime?)] = SqlDbType.DateTime,
			[typeof(DateTimeOffset?)] = SqlDbType.DateTimeOffset,
			[typeof(Decimal?)] = SqlDbType.Decimal,
			[typeof(Double?)] = SqlDbType.Float,
			[typeof(Int32?)] = SqlDbType.Int,
			[typeof(Single?)] = SqlDbType.Real,
			[typeof(Int16?)] = SqlDbType.SmallInt,
			[typeof(Byte?)] = SqlDbType.TinyInt,
			[typeof(Guid?)] = SqlDbType.UniqueIdentifier,
			[typeof(RowVersion?)] = SqlDbType.VarBinary

			// [typeof(String)] = SqlDbType.Char,
			// [typeof(Char[])] = SqlDbType.Char,
			// [typeof(DateTime)] = SqlDbType.Date,
			// [typeof(DateTime)] = SqlDbType.DateTime2,
			// [typeof(Byte[])] = SqlDbType.Binary,
			// [typeof(String)] = SqlDbType.NChar,
			// [typeof(Char[])] = SqlDbType.NChar,
			// [typeof(String)] = SqlDbType.NText,
			// [typeof(String)] = SqlDbType.NVarChar,
			// [typeof(Char[])] = SqlDbType.NVarChar,
			// [typeof(Byte[])] = SqlDbType.Timestamp,
			// [typeof(Decimal)] = (SqlDbType.SmallMoney),
			// [typeof(Object)] = (SqlDbType.Variant),
			// [typeof(String)] = SqlDbType.Text,
			// [typeof(Char[])] = SqlDbType.Text,
			// [typeof(TimeSpan)] = (SqlDbType.Time),
			// [typeof(Byte[])] = SqlDbType.Timestamp,
			// [typeof(Byte[])] = SqlDbType.VarBinary,
			// [typeof(Char[])] = SqlDbType.VarChar
			// [typeof(Xml)] = SqlDbType.Xml
		};
		internal static Dictionary<SqlDbType, Expression> SqlTypeToReaderMap = new Dictionary<SqlDbType, Expression>
		{
			[SqlDbType.BigInt] = _getSqlInt64,
			[SqlDbType.VarBinary] = _getSqlBinary,
			[SqlDbType.Bit] = _getSqlBoolean,
			[SqlDbType.DateTime] = _getSqlDateTime,
			[SqlDbType.DateTimeOffset] = _getDateTimeOffset,
			[SqlDbType.Decimal] = _getSqlDecimal,
			[SqlDbType.Float] = _getSqlDouble,
			[SqlDbType.Int] = _getSqlInt32,
			[SqlDbType.Real] = _getSqlSingle,
			[SqlDbType.SmallInt] = _getSqlInt16,
			[SqlDbType.TinyInt] = _getSqlByte,
			[SqlDbType.UniqueIdentifier] = _getSqlGuid,
			[SqlDbType.VarChar] = _getSqlString,
			[SqlDbType.Money] = _getSqlMoney
			// [typeof(String)] = SqlDbType.Char,
			// [typeof(Char[])] = SqlDbType.Char,
			// [typeof(DateTime)] = SqlDbType.Date,
			// [typeof(DateTime)] = SqlDbType.DateTime2,
			// [typeof(Byte[])] = SqlDbType.Binary,
			// [typeof(String)] = SqlDbType.NChar,
			// [typeof(Char[])] = SqlDbType.NChar,
			// [typeof(String)] = SqlDbType.NText,
			// [typeof(String)] = SqlDbType.NVarChar,
			// [typeof(Char[])] = SqlDbType.NVarChar,
			// [typeof(Byte[])] = SqlDbType.Timestamp,
			// [typeof(Decimal)] = (SqlDbType.SmallMoney),
			// [typeof(Object)] = (SqlDbType.Variant),
			// [typeof(String)] = SqlDbType.Text,
			// [typeof(Char[])] = SqlDbType.Text,
			// [typeof(TimeSpan)] = (SqlDbType.Time),
			// [typeof(Byte[])] = SqlDbType.Timestamp,
			// [typeof(Byte[])] = SqlDbType.VarBinary,
			// [typeof(Char[])] = SqlDbType.VarChar
			// [typeof(Xml)] = SqlDbType.Xml
		};
		#endregion
	}
}
