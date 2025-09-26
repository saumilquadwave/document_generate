namespace econsys.DocGenServiceSTA.Constants
{
    public class CommonConstants
    {
        public const string UnresolvedReplacerText = "";
        public const string Variable_Prefix = "<$";
        public const string Variable_Suffix = "$>";
    }

    public enum EnumDocumentContentType
    {
        InlineContent = 0,
        File = 1
    }   

    public enum ResponseFormatType
    {
        Docx = 0,
        Pdf = 1
    }
}
