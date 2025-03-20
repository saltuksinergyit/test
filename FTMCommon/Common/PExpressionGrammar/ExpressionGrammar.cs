using Irony.Parsing;


public class ExpressionGrammar : Grammar
{
    public ExpressionGrammar()
    {
        // 🔹 Özel Değişken Tanımı (`$.x.y` desteği için)
        var variable = new RegexBasedTerminal("variable", @"\${1,2}\.[a-zA-Z_][a-zA-Z0-9_.]*");


        // 🔹 String Literalleri (`'Test'` gibi)
        var strLiteral = new StringLiteral("string", "'", StringOptions.NoEscapes);

        // 🔹 Sayılar (1, 23, 45.6 gibi)
        var number = new NumberLiteral("number");

        // 🔹 Fonksiyonlar (Now() gibi)
        var nowFunc = ToTerm("Now") + "(" + ")";

        // 🔹 Matematiksel Operatörler
        var plus = ToTerm("+");
        var minus = ToTerm("-");
        var multiply = ToTerm("*");
        var divide = ToTerm("/");
        var mod = ToTerm("%");


        var lparen = ToTerm("(");
        var rparen = ToTerm(")");

        // 🔹 Karşılaştırma Operatörleri
        var eq = ToTerm("="); var neq = ToTerm("!=");
        var gt = ToTerm(">"); var lt = ToTerm("<");
        var gte = ToTerm(">="); var lte = ToTerm("<=");

        // 🔹 Ternary (Koşullu) Operatörler
        var questionMark = ToTerm("?"); var colon = ToTerm(":");

        // 🔹 NonTerminals (Gramer Kuralları)
        var expr = new NonTerminal("expr");
        var condition = new NonTerminal("condition");
        var ternary = new NonTerminal("ternary");

        // 🔹 Matematiksel işlemler
        expr.Rule = lparen + expr + rparen | expr + mod + expr | expr + plus + expr | expr + minus + expr | expr + multiply + expr | expr + divide + expr |
                    variable | strLiteral | number | nowFunc | ternary;

        // 🔹 Karşılaştırma işlemleri **öncelikli oldu**
        condition.Rule = expr + eq + expr | expr + neq + expr |
                         expr + gt + expr | expr + lt + expr |
                         expr + gte + expr | expr + lte + expr;

        // 🔹 Ternary (Koşullu ifadeler)
        ternary.Rule = "(" + condition + ")" + questionMark + expr + colon + expr;

        // 🔹 Kök (Root)
        var root = new NonTerminal("root");
        root.Rule = condition | expr;
        Root = root;
    }
}
