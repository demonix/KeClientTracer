using System;
using System.IO;
using System.Linq.Expressions;

namespace Common
{
    public static class StreamReaderExtensions
    {
        static readonly Func<StreamReader, int> CharLenAcc = GetFieldAccessor<StreamReader, int>("charLen");
        static readonly Func<StreamReader, int> CharPosAcc = GetFieldAccessor<StreamReader, int>("charPos");

        public static Func<T, R> GetFieldAccessor<T, R>(string fieldName)
        {
            var param = Expression.Parameter(typeof(T), "arg");
            var member = Expression.Field(param, fieldName);
            var lambda = Expression.Lambda(typeof(Func<T, R>), member, param);
            var compiled = (Func<T, R>)lambda.Compile();
            return compiled;
        }

        public static long GetRealPosition(this StreamReader sr)
        {
            return sr.BaseStream.Position - CharLenAcc(sr) + CharPosAcc(sr);
        }
    }
}