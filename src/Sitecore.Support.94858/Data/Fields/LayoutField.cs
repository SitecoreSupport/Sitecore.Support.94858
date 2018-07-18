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

    /// <summary>Removes the link.</summary>
    /// <param name="itemLink">The item link.</param>
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
          for (int num = devices.Count - 1; num >= 0; num--)
          {
            DeviceDefinition deviceDefinition = devices[num] as DeviceDefinition;
            if (deviceDefinition != null)
            {
              if (!(deviceDefinition.ID == b))
              {
                if (deviceDefinition.Layout == b)
                {
                  deviceDefinition.Layout = null;
                  continue;
                }
                if (deviceDefinition.Placeholders != null)
                {
                  string targetPath = itemLink.TargetPath;
                  bool flag = false;
                  for (int num2 = deviceDefinition.Placeholders.Count - 1; num2 >= 0; num2--)
                  {
                    PlaceholderDefinition placeholderDefinition = deviceDefinition.Placeholders[num2] as PlaceholderDefinition;
                    if (placeholderDefinition != null && (string.Equals(placeholderDefinition.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) || string.Equals(placeholderDefinition.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                    {
                      deviceDefinition.Placeholders.Remove(placeholderDefinition);
                      flag = true;
                    }
                  }
                  if (!flag)
                  {
                    goto IL_0119;
                  }
                  continue;
                }
                goto IL_0119;
              }
              devices.Remove(deviceDefinition);
            }
            continue;
            IL_0119:
            if (deviceDefinition.Renderings != null)
            {
              for (int num3 = deviceDefinition.Renderings.Count - 1; num3 >= 0; num3--)
              {
                RenderingDefinition renderingDefinition = deviceDefinition.Renderings[num3] as RenderingDefinition;
                if (renderingDefinition != null)
                {
                  if (renderingDefinition.Datasource == itemLink.TargetPath)
                  {
                    renderingDefinition.Datasource = string.Empty;
                  }
                  if (renderingDefinition.ItemID == b)
                  {
                    deviceDefinition.Renderings.Remove(renderingDefinition);
                  }
                  if (renderingDefinition.Datasource == b)
                  {
                    renderingDefinition.Datasource = string.Empty;
                  }
                  if (!string.IsNullOrEmpty(renderingDefinition.Parameters))
                  {
                    Item item = base.InnerField.Database.GetItem(renderingDefinition.ItemID);
                    if (item != null)
                    {
                      RenderingParametersFieldCollection parametersFields = GetParametersFields(item, renderingDefinition.Parameters);
                      foreach (CustomField value2 in parametersFields.Values)
                      {
                        if (!string.IsNullOrEmpty(value2.Value))
                        {
                          value2.RemoveLink(itemLink);
                        }
                      }
                      renderingDefinition.Parameters = parametersFields.GetParameters().ToString();
                    }
                  }
                }
              }
            }
          }
          base.Value = layoutDefinition.ToXml();
        }
      }
    }

    /// <summary>
    /// Gets the parameters fields.
    /// </summary>
    /// <param name="layoutItem">The layout item.</param>
    /// <param name="renderingParameters">The rendering parameters.</param>
    /// <returns></returns>
    private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
    {
      UrlString parameters = new UrlString(renderingParameters);
      RenderingParametersFieldCollection parametersFields;
      RenderingParametersFieldCollection.TryParse(layoutItem, parameters, out parametersFields);
      return parametersFields;
    }
  }
}
