using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SWBF2Admin.Web
{
    public class EncodeHtmlResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);

            foreach (JsonProperty prop in props.Where(p => p.PropertyType == typeof(string)))
            {
                PropertyInfo pi = type.GetProperty(prop.UnderlyingName);
                if (pi != null)
                {
                    prop.ValueProvider = new HtmlEncodingValueProvider(pi);
                }
            }

            return props;
        }

        protected class HtmlEncodingValueProvider : IValueProvider
        {
            PropertyInfo targetProperty;

            public HtmlEncodingValueProvider(PropertyInfo targetProperty)
            {
                this.targetProperty = targetProperty;
            }

            public void SetValue(object target, object value)
            {
                targetProperty.SetValue(target, (string)value);
            }

            public object GetValue(object target)
            {
                string value = (string)targetProperty.GetValue(target);
                return WebUtility.HtmlEncode(value);
            }
        }
    }
}