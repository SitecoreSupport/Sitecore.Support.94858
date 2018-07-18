namespace Sitecore.Support.Data.Fields
{
  using Sitecore.Data.Fields;
  using Sitecore.Diagnostics;
  using Sitecore.Links;
  using System;

  /// <summary>
  /// Represents a Lookup field.
  /// </summary>
  public class ValueLookupField : Sitecore.Data.Fields.ValueLookupField
  {
    /// <summary>
    /// Creates a new <see cref="T:Sitecore.Data.Fields.LookupField" /> instance.
    /// </summary>
    /// <param name="innerField">Inner field.</param>
    /// <contract>
    ///   <requires name="innerField" condition="none" />
    /// </contract>
    public ValueLookupField(Field innerField)
        : base(innerField)
    {
    }

    /// <summary>
    /// Removes the link.
    /// </summary>
    /// <param name="itemLink">The item link.</param>
    public override void RemoveLink(ItemLink itemLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      #region Modified code
      if (this.HasLink(itemLink))
      {
        Clear();
      }
      #endregion
    }

    #region Added code
    /// <summary>
    /// Check if the field has a link to the passed itemlink
    /// </summary>
    /// <param name="itemLink">The item link that should be checked against the field value.</param>
    /// <returns>The result</returns>
    private bool HasLink(ItemLink itemLink)
    {
      Assert.IsNotNull(itemLink, nameof(itemLink));
      if (string.IsNullOrEmpty(this.Value)) return false;
      return Sitecore.Support.StringUtil.Contains(this.Value, itemLink.TargetPath, StringComparison.OrdinalIgnoreCase) ||
             Sitecore.Support.StringUtil.Contains(this.Value, itemLink.TargetItemID.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    #endregion
  }
}