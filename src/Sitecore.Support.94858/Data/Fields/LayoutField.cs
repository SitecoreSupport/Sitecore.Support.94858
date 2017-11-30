using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Text;
using Sitecore.Xml;
using System;
using System.Collections;
using System.Xml;
/// <summary>
/// only RemoveLink method was changed
/// in order to make the patch compatible with more versions, the class is inherited from original LayoutField
/// </summary>
namespace Sitecore.Support.Data.Fields
{
  public class LayoutField : Sitecore.Data.Fields.LayoutField
  {
    private readonly XmlDocument data;

    public LayoutField(Field innerField) : base(innerField)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      this.data = this.LoadData();
    }

    private XmlDocument LoadData()
    {
      string value = base.Value;
      XmlDocument result;
      if (!string.IsNullOrEmpty(value))
      {
        result = XmlUtil.LoadXml(value);
      }
      else
      {
        result = XmlUtil.LoadXml("<r/>");
      }
      return result;
    }

    public LayoutField(Item item) : this(item.Fields[FieldIDs.LayoutField])
    {
    }

    public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      Assert.ArgumentNotNullOrEmpty(runtimeValue, "runtimeValue");
      this.data = this.LoadData();
    }

    public override void RemoveLink(ItemLink itemLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      string value = base.Value;
      if (!string.IsNullOrEmpty(value))
      {
        LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
        ArrayList devices = layoutDefinition.Devices;
        if (devices != null)
        {
          string b = itemLink.TargetItemID.ToString();
          int i = devices.Count - 1;
          while (i >= 0)
          {
            DeviceDefinition deviceDefinition = devices[i] as DeviceDefinition;
            if (deviceDefinition != null)
            {
              if (deviceDefinition.ID == b)
              {
                devices.Remove(deviceDefinition);
              }
              else if (deviceDefinition.Layout == b)
              {
                deviceDefinition.Layout=(null);
              }
              else
              {
                if (deviceDefinition.Placeholders != null)
                {
                  string targetPath = itemLink.TargetPath;
                  bool flag = false;
                  for (int j = deviceDefinition.Placeholders.Count - 1; j >= 0; j--)
                  {
                    PlaceholderDefinition placeholderDefinition = deviceDefinition.Placeholders[j] as PlaceholderDefinition;
                    if (placeholderDefinition != null && (string.Equals(placeholderDefinition.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) || string.Equals(placeholderDefinition.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                    {
                      deviceDefinition.Placeholders.Remove(placeholderDefinition);
                      flag = true;
                    }
                  }
                  if (flag)
                  {
                    goto IL_309;
                  }
                }
                if (deviceDefinition.Renderings != null)
                {
                  for (int k = deviceDefinition.Renderings.Count - 1; k >= 0; k--)
                  {
                    RenderingDefinition renderingDefinition = deviceDefinition.Renderings[k] as RenderingDefinition;
                    if (renderingDefinition != null)
                    {
                      if (renderingDefinition.Datasource == itemLink.TargetPath)
                      {
                        renderingDefinition.Datasource=(string.Empty);
                      }
                      if (renderingDefinition.ItemID == b)
                      {
                        deviceDefinition.Renderings.Remove(renderingDefinition);
                      }
                      if (renderingDefinition.Datasource == b)
                      {
                        renderingDefinition.Datasource=(string.Empty);
                      }
                      if (!string.IsNullOrEmpty(renderingDefinition.Parameters))
                      {
                        Item item = base.InnerField.Database.GetItem(renderingDefinition.ItemID);
                        if (item != null)
                        {
                          RenderingParametersFieldCollection parametersFields = this.GetParametersFields(item, renderingDefinition.Parameters);
                          foreach (CustomField current in parametersFields.Values)
                          {
                            if (!string.IsNullOrEmpty(current.Value))
                            {
                              current.RemoveLink(itemLink);
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            IL_309:
            i--;
            continue;
            goto IL_309;
          }
          base.Value=(layoutDefinition.ToXml());
        }
      }
    }

    private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
    {
      UrlString urlString = new UrlString(renderingParameters);
      RenderingParametersFieldCollection result;
      RenderingParametersFieldCollection.TryParse(layoutItem, urlString, out result);
      return result;
    }
  }
}
