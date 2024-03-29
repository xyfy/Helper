using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Xyfy.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExpressionHelper
    {
        internal static string CharCodeAt(this string character, int index)
        {
            return (character[index] + "").CharCodeAt();
        }

        internal static string CharCodeAt(this string character)
        {
            string coding = "";
            for (int i = 0; i < character.Length; i++)
            {
                byte[] bytes = System.Text.Encoding.Unicode.GetBytes(character.Substring(i, 1));
                //Fetching binary encoded content  
                string lowCode = System.Convert.ToString(bytes[1], 16);
                if (lowCode.Length == 1)
                {
                    lowCode = "0" + lowCode;
                }
                string hightCode = System.Convert.ToString(bytes[0], 16);
                if (hightCode.Length == 1)
                {
                    hightCode = "0" + hightCode;
                }
                coding += (lowCode + hightCode);
            }
            return coding;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HashToAnchorString(this string str)
        {
            var slug = str.Trim().ToLower();
            slug = Regex.Replace(slug, @"[\s,.[\]{}()/]+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9 -]", delegate (Match m)
            {
                return m.Value.CharCodeAt();
            });
            slug = Regex.Replace(slug, @"-{2,}", "-");
            slug = Regex.Replace(slug, @"^-*|-*$", "");
            if (Regex.Match(slug[0].ToString(), @"[^a-z]").Success)
            {
                slug = "section-" + slug;
            }
            return HttpUtility.UrlEncode(slug);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="t"></param>
        /// <param name="selector"></param>
        /// <param name="newValue"></param>
        public static void SetPropertyValue<T, TProperty>(this T t, Expression<Func<T, TProperty>> selector, TProperty newValue)
        {
            var valueType = typeof(TProperty);
            ParameterExpression param_val = Expression.Parameter(typeof(T), "x");
            var valueExpress = Expression.Constant(newValue, valueType);
            if (selector.Body is MemberExpression memberExpression)
            {
                memberExpression = Expression.Property(Expression.Constant(t),memberExpression.Member.Name);
                var assignExpression = Expression.Assign(memberExpression, valueExpress);
                var lambda =
                   Expression.Lambda<Func<TProperty>>(assignExpression);
                lambda.Compile()();
            }
        }

        public static void SetPropertyValue<T, TProperty>(this T t, string propertyName, TProperty newValue)
        {
            var valueType = typeof(TProperty);
            var valueExpress = Expression.Constant(newValue, valueType);
            MemberExpression member = Expression.Property(Expression.Constant(t), propertyName);
            var assignExpression = Expression.Assign(member, valueExpress);
            var lambda =
               Expression.Lambda<Func<TProperty>>(assignExpression);
            lambda.Compile()();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void SetPropertyValue<T>(this T t, string name, object value)
        {
            Type type = t!.GetType();
            if (type == null)
            {
                return;
            }
            PropertyInfo p = type?.GetProperty(name);
            if (p == null)
            {
                throw new ArgumentException(name);
            }
            var param_obj = Expression.Parameter(type!);
            var param_val = Expression.Parameter(typeof(object));
            var body_obj = Expression.Convert(param_obj, type!);
            var body_val = Expression.Convert(param_val, p.PropertyType);
            var setMethod = p.GetSetMethod(true);
            if (setMethod != null)
            {
                var body = Expression.Call(param_obj, setMethod, body_val);
                var setValue = Expression.Lambda<Action<T, object>>(body, param_obj, param_val).Compile();
                setValue(t, value);
            }
        }
    }
}
