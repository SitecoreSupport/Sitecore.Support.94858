namespace Sitecore.Support.Data.Fields
{
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Layouts;
  using Sitecore.Links;
  using Sitecore.Text;
  using Sitecore.Xml;
  using System;
  using System.Collections;
  using System.Reflection;
  using System.Xml;
  using System.Xml.Linq;

  public class LayoutField : Sitecore.Data.Fields.LayoutField
  {
    private readonly XmlDocument data;

    public LayoutField(Field innerField) : base(innerField)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      this.data = this.LoadData();
    }

    public LayoutField(Sitecore.Data.Items.Item item) : this(item.Fields[FieldIDs.FinalLayoutField])
    {
    }

    public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      Assert.ArgumentNotNullOrEmpty(runtimeValue, "runtimeValue");
      this.data = this.LoadData();
    }

    private RenderingParametersFieldCollection GetParametersFields(Sitecore.Data.Items.Item layoutItem, string renderingParameters)
    {
      RenderingParametersFieldCollection fields;
      UrlString parameters = new UrlString(renderingParameters);
      RenderingParametersFieldCollection.TryParse(layoutItem, parameters, out fields);
      return fields;
    }

    private XmlDocument LoadData()
    {
      string str = base.Value;
      if (!string.IsNullOrEmpty(str))
      {
        return XmlUtil.LoadXml(str);
      }
      return XmlUtil.LoadXml("<r/>");
    }

    public override void RemoveLink(ItemLink itemLink)
    {

      Assert.ArgumentNotNull(itemLink, "itemLink");

      string str = base.Value;
      if (!string.IsNullOrEmpty(str))
      {
        LayoutDefinition definition = LayoutDefinition.Parse(str);
        ArrayList devices = definition.Devices;
        if (devices != null)
        {
          string b = itemLink.TargetItemID.ToString();
          for (int i = devices.Count - 1; i >= 0; i--)
          {
            DeviceDefinition definition2 = devices[i] as DeviceDefinition;
            if (definition2 != null)
            {
              if (definition2.ID == b)
              {
                devices.Remove(definition2);
              }
              else if (definition2.Layout == b)
              {
                definition2.Layout = null;
              }
              else
              {
                if (definition2.Placeholders != null)
                {
                  string targetPath = itemLink.TargetPath;
                  bool flag = false;
                  for (int j = definition2.Placeholders.Count - 1; j >= 0; j--)
                  {
                    PlaceholderDefinition definition3 = definition2.Placeholders[j] as PlaceholderDefinition;
                    if ((definition3 != null) && (string.Equals(definition3.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) || string.Equals(definition3.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                    {
                      definition2.Placeholders.Remove(definition3);
                      flag = true;
                    }
                  }
                  if (flag)
                  {
                    continue;
                  }
                }
                if (definition2.Renderings != null)
                {
                  for (int k = definition2.Renderings.Count - 1; k >= 0; k--)
                  {
                    RenderingDefinition definition4 = definition2.Renderings[k] as RenderingDefinition;
                    if (definition4 != null)
                    {
                      if (definition4.Datasource == itemLink.TargetPath)
                      {
                        definition4.Datasource = string.Empty;
                      }
                      if (definition4.ItemID == b)
                      {
                        definition2.Renderings.Remove(definition4);
                      }
                      if (definition4.Datasource == b)
                      {
                        definition4.Datasource = string.Empty;
                      }
                      if (!string.IsNullOrEmpty(definition4.Parameters))
                      {
                        Sitecore.Data.Items.Item layoutItem = base.InnerField.Database.GetItem(definition4.ItemID);
                        if (layoutItem != null)
                        {
                          foreach (CustomField field in this.GetParametersFields(layoutItem, definition4.Parameters).Values)
                          {
                            if (!string.IsNullOrEmpty(field.Value))
                            {
                              //The fix: assign editing mode to Standart values item
                              field.InnerField.Item.Editing.BeginEdit();
                              field.RemoveLink(itemLink);
                              field.InnerField.Item.Editing.EndEdit();
                            }
                          }
                        }
                      }
                      if (definition4.Rules != null)
                      {
                        RulesField field2 = new RulesField(base.InnerField, definition4.Rules.ToString());
                        field2.RemoveLink(itemLink);
                        definition4.Rules = XElement.Parse(field2.Value);
                      }
                    }
                  }
                }
              }
            }
          }
          base.Value = definition.ToXml();
        }
      }
    }
  }
}
