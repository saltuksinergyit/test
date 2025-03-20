using System.Dynamic;

namespace FTMCommon.Common.MainService
{
    public class ExpressionServices
    {
        private ExpressionEvaluator evaluator;
        private ExpressionValue entity;

        public static dynamic ConvertToDynamic(List<KeyValuePair<string, object>> keyValuePairs)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;

            foreach (var kv in keyValuePairs)
            {
                if (kv.Value is List<KeyValuePair<string, object>> nestedList)
                {
                    expando[kv.Key] = ConvertToDynamic(nestedList); // 🔄 Recursive olarak içeri giriyoruz
                }
                else if (kv.Value is KeyValuePair<string, object>)
                {
                    var x = new List<KeyValuePair<string, object>>();
                    x.Add((KeyValuePair<string, object>)kv.Value);

                    expando[kv.Key] = ConvertToDynamic(x); // 🔄 Recursive olarak içeri giriyoruz
                }
                else
                {
                    expando[kv.Key] = kv.Value;
                }
            }

            return (ExpandoObject)expando;
        }


        public ExpressionServices()
        {
            this.evaluator = new ExpressionEvaluator();

            // Dinamik bir Entity tanımlayalım
            this.entity = new ExpressionValue
            {
                //o = new
                //{
                //    FlowId = 456,
                //    FlowName = "SampleFlow",
                //    UserName = "JohnDoe",
                //    x = new
                //    {
                //        y = 1
                //    }
                //},
                //fi = new
                //{
                //    item = new { t = 2 },
                //    x = 1
                //},
                global = ConvertToDynamic(new List<KeyValuePair<string, object>>
                    {
                        new KeyValuePair<string, object>("WorkingDir","C:\\x\\y"),
                        new KeyValuePair<string, object>("x",new KeyValuePair<string,object>("x","1")),
                    })

            };
        }



        public T GetExpression<T>(string expression, object entitiy, object forData,T? defaultValue=default) 
        {
            try
            {
                this.entity.o = entitiy;
                this.entity.fi = forData;
                this.evaluator.SetEntity(this.entity);
                return this.evaluator.Evaluate<T>(expression);
            }
            catch (Exception)
            {
                if (defaultValue != null)
                    return defaultValue;
                throw;
            }
        }
    }

    internal class ExpressionValue
    {
        public dynamic o { get; set; }
        public dynamic global { get; set; }
        public dynamic fi { get; set; }
    }
}
