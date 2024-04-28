
using System.Text.RegularExpressions;

public class UUIDHandler
{
    public bool IsUUIDValid(string value)
    {
        string pattern = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
        Regex rg = new(pattern);

        return rg.IsMatch(value);
    }
}
