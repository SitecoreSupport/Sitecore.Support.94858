namespace Sitecore.Support.Data.Fields
{
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Links;
  using Sitecore.Web.UI.HtmlControls.Data;

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
      Clear();
    }
  }
}