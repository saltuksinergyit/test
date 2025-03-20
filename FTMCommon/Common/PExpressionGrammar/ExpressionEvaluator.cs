using System.Dynamic;
using Irony.Parsing;

public class ExpressionEvaluator
{
    private readonly Parser _parser;
    private readonly Dictionary<string, object> _variables;

    public ExpressionEvaluator()
    {
        var grammar = new ExpressionGrammar();
        _parser = new Parser(grammar);
        _variables = new Dictionary<string, object>();
    }

    public void SetEntity(object entity)
    {
        _variables.Clear();
        AddProperties(_variables, entity, "$");
    }


    public static void GetProperties(Dictionary<string, object> dictionaryValues, ExpandoObject expando, string parentKey = "")
    {
        var dictionary = (IDictionary<string, object>)expando;

        foreach (var kv in dictionary)
        {
            string fullKey = string.IsNullOrEmpty(parentKey) ? kv.Key : $"{parentKey}.{kv.Key}";
            dictionaryValues[parentKey + "." + kv.Key] = kv.Value;

            if (kv.Value is ExpandoObject nestedExpando)
            {
                GetProperties(dictionaryValues, nestedExpando, fullKey); // 🔄 Eğer iç içe ExpandoObject varsa devam et
            }
        }
    }



    private void AddProperties(Dictionary<string, object> dictionary, object obj, string prefix)
    {
        if (obj == null) return;

        foreach (var prop in obj.GetType().GetProperties())
        {
            try
            {


                object value = prop.GetValue(obj);
                if (prop.Name == "global")
                {
                    GetProperties(dictionary, value as ExpandoObject, "$$");
                    continue;

                }
                string varName = prefix + "." + prop.Name;
                bool isPrefix = true;
                if (varName.StartsWith("$.o"))
                {
                    isPrefix = false;
                    varName = varName.Replace("$.o", "$");
                }
                if (varName.StartsWith("$.fi"))
                {
                    isPrefix = false;
                    varName = varName.Replace("$.fi", "$.for");
                }

                dictionary[varName] = value ?? "";

                if (value != null && !IsPrimitive(value))
                {
                    AddProperties(dictionary, value, varName);
                }

            }
            catch (Exception ex)
            {

            }
        }
    }

    private bool IsPrimitive(object obj)
    {
        return obj is string || obj is ValueType;
    }

    public T Evaluate<T>(string expression) // bool, decimal, string, DateTime 
    {
        var parseTree = _parser.Parse(expression);
        if (parseTree.HasErrors())
        {
            throw new Exception("Geçersiz ifade!");
        }
        var result = EvaluateNode(parseTree.Root);
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)(result == "True");
        }
        if (typeof(T) == typeof(string))
        {
            return (T)(object)result;
        }
        if (typeof(T) == typeof(decimal))
        {
            decimal.TryParse(result, out decimal decimalResult);
            return (T)(object)decimalResult;
        }
        if (typeof(T) == typeof(DateTime))
        {
            DateTime.TryParse(result, out DateTime dateTimeResult);
            return (T)(object)dateTimeResult;
        }
        return default;
    }

    private string EvaluateNode(ParseTreeNode node)
    {
        if (node.Token != null)
        {
            if (node.Term.Name == "string") return node.Token.ValueString;
            if (node.Term.Name == "number") return node.Token.ValueString;
            if (node.Term.Name == "variable")
            {
                string varName = node.Token.ValueString;
                return _variables.TryGetValue(varName, out var value) ? value.ToString() : throw new Exception($"Tanımsız değişken: {varName}");
            }
        }
        if (node.ChildNodes.Count == 3 && node.ChildNodes[0].Token?.ValueString == "(")
        {
            return EvaluateNode(node.ChildNodes[1]);
        }

        if (node.Term.Name == "expr" && node.ChildNodes.Count == 3 && node.ChildNodes[0].Term.Name == "Now")
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        if (node.Term.Name == "condition" || (node.Term.Name == "expr" && node.ChildNodes.Count == 3))
        {
            string left = EvaluateNode(node.ChildNodes[0]);
            string op = node.ChildNodes[1].Token.ValueString;
            string right = EvaluateNode(node.ChildNodes[2]);

            bool isLeftNumeric = double.TryParse(left, out double leftNum);
            bool isRightNumeric = double.TryParse(right, out double rightNum);

            if (op == "+")
            {
                return isLeftNumeric && isRightNumeric ? (leftNum + rightNum).ToString() : left + right;
            }
            if (op == "-")
            {
                return isLeftNumeric && isRightNumeric ? (leftNum - rightNum).ToString() : throw new Exception("type error");
            }
            if (op == "/")
            {
                return isLeftNumeric && isRightNumeric ? (leftNum / rightNum).ToString() : throw new Exception("type error");
            }
            if (op == "*")
            {
                return isLeftNumeric && isRightNumeric ? (leftNum * rightNum).ToString() : throw new Exception("type error");
            }
            if (op == "%")
            {
                return isLeftNumeric && isRightNumeric ? (leftNum % rightNum).ToString() : throw new Exception("type error");
            }

            // 🔹 **Karşılaştırma Operatörleri**
            if (isLeftNumeric && isRightNumeric)
            {
                return op switch
                {
                    "=" => (leftNum == rightNum).ToString(),
                    "!=" => (leftNum != rightNum).ToString(),
                    ">" => (leftNum > rightNum).ToString(),
                    "<" => (leftNum < rightNum).ToString(),
                    ">=" => (leftNum >= rightNum).ToString(),
                    "<=" => (leftNum <= rightNum).ToString(),
                    _ => throw new Exception($"Geçersiz operatör: {op}")
                };
            }
            else
            {
                return op switch
                {
                    "=" => (left == right).ToString(),
                    "!=" => (left != right).ToString(),
                    _ => throw new Exception($"Geçersiz operatör: {op}")
                };
            }
        }

        if (node.ChildNodes.Count == 5 && node.ChildNodes[1].Token?.ValueString == "?")
        {
            string conditionResult = EvaluateNode(node.ChildNodes[0]);
            return conditionResult == "True" ? EvaluateNode(node.ChildNodes[2]) : EvaluateNode(node.ChildNodes[4]);
        }

        return EvaluateNode(node.ChildNodes[0]);
    }
}
