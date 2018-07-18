namespace Sitecore.Support
{
  using System;
  
  public sealed class StringUtil
  {
    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within the text.
    /// </summary>
    /// <param name="text">Thr base string</param>
    /// <param name="substr">The value to check</param>
    /// <param name="comp">Specifies the culture, case, and sort rules to be used</param>
    public static bool Contains(string text, string substr, StringComparison comp)
    {
      return text != null && text.IndexOf(substr, 0, comp) >= 0;
    }
  }
}
