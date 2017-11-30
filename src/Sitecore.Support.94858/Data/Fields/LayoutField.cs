using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetLayoutSourceFields;
using Sitecore.Pipelines.ResolveRenderingDatasource;
using Sitecore.Text;
using Sitecore.Xml;
using Sitecore.Xml.Patch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Sitecore.Data;
using Sitecore.Data.Fields;

namespace Sitecore.Support.Data.Fields
{
  /// <summary>Represents a Layout field.</summary>
  public class LayoutField : CustomField
  {
    /// <summary>
    /// Specifies empty value for the layout field.
    /// </summary>
    public const string EmptyValue = "<r />";

    /// <summary>The data.</summary>
    private readonly System.Xml.XmlDocument data;

    /// <summary>
    /// Gets the XML data document.
    /// </summary>
    /// <value>The data.</value>
    public System.Xml.XmlDocument Data
    {
      get
      {
        return this.data;
      }
    }

    /// <summary>Initializes a new instance of the <see cref="T:Sitecore.Data.Fields.LayoutField" /> class. Creates LayoutField from specific item.</summary>
    /// <param name="item">Item to get layout for.</param>
    public LayoutField(Item item) : this(item.Fields[FieldIDs.FinalLayoutField])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:Sitecore.Data.Fields.LayoutField" /> class. Creates a new <see cref="T:Sitecore.Data.Fields.LayoutField" /> instance.</summary>
    /// <param name="innerField">Inner field.</param>
    public LayoutField(Field innerField) : base(innerField)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      this.data = this.LoadData();
    }

    /// <summary>Initializes a new instance of the <see cref="T:Sitecore.Data.Fields.LayoutField" /> class.</summary>
    /// <param name="innerField">The inner field.</param>
    /// <param name="runtimeValue">The runtime value.</param>
    public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
      Assert.ArgumentNotNullOrEmpty(runtimeValue, "runtimeValue");
      this.data = this.LoadData();
    }

    /// <summary>
    /// Converts a <see cref="T:Sitecore.Data.Fields.Field" /> to a <see cref="T:Sitecore.Data.Fields.LayoutField" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The implicit operator.</returns>
    public static implicit operator LayoutField(Field field)
    {
      if (field != null)
      {
        return new LayoutField(field);
      }
      return null;
    }

    /// <summary>Extracts the layout ID.</summary>
    /// <param name="deviceNode">Device node.</param>
    /// <returns>The layout ID.</returns>
    public static ID ExtractLayoutID(System.Xml.XmlNode deviceNode)
    {
      Assert.ArgumentNotNull(deviceNode, "deviceNode");
      string attribute = XmlUtil.GetAttribute("l", deviceNode);
      if (attribute.Length > 0 && ID.IsID(attribute))
      {
        return ID.Parse(attribute);
      }
      return ID.Null;
    }

    /// <summary>Extracts the Rendering references.</summary>
    /// <param name="deviceNode">Device node.</param>
    /// <param name="language">Language.</param>
    /// <param name="database">Database.</param>
    /// <returns>The references.</returns>
    public static RenderingReference[] ExtractReferences(System.Xml.XmlNode deviceNode, Language language, Database database)
    {
      Assert.ArgumentNotNull(deviceNode, "deviceNode");
      Assert.ArgumentNotNull(language, "language");
      Assert.ArgumentNotNull(database, "database");
      System.Xml.XmlNodeList xmlNodeList = deviceNode.SelectNodes("r");
      Assert.IsNotNull(xmlNodeList, "nodes");
      RenderingReference[] array = new RenderingReference[xmlNodeList.Count];
      for (int i = 0; i < xmlNodeList.Count; i++)
      {
        array[i] = new RenderingReference(xmlNodeList[i], language, database);
      }
      return array;
    }

    /// <summary>Gets the field value, applying any layout deltas.</summary>
    /// <param name="field">The field to get value for.</param>
    /// <returns>The calculated layout value.</returns>
    public static string GetFieldValue(Field field)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.IsTrue(field.ID == FieldIDs.LayoutField || field.ID == FieldIDs.FinalLayoutField, "The field is not a layout/renderings field");
      GetLayoutSourceFieldsArgs getLayoutSourceFieldsArgs = new GetLayoutSourceFieldsArgs(field);
      bool flag = GetLayoutSourceFieldsPipeline.Run(getLayoutSourceFieldsArgs);
      List<string> list = new List<string>();
      if (flag)
      {
        list.AddRange(getLayoutSourceFieldsArgs.FieldValuesSource.Select(delegate (Field fieldValue)
        {
          string arg_22_0;
          if ((arg_22_0 = fieldValue.GetValue(false, false)) == null)
          {
            arg_22_0 = (fieldValue.GetInheritedValue(false) ?? fieldValue.GetValue(false, false, true, false, false));
          }
          return arg_22_0;
        }));
        list.AddRange(from fieldValue in getLayoutSourceFieldsArgs.StandardValuesSource
          select fieldValue.GetStandardValue());
      }
      else
      {
        list = LayoutField.DoGetFieldValue(field);
      }
      System.Collections.Generic.Stack<string> stack = new System.Collections.Generic.Stack<string>();
      string text = null;
      foreach (string current in list)
      {
        if (!string.IsNullOrWhiteSpace(current))
        {
          if (!XmlPatchUtils.IsXmlPatch(current))
          {
            text = current;
            break;
          }
          stack.Push(current);
        }
      }
      if (string.IsNullOrWhiteSpace(text))
      {
        return string.Empty;
      }
      return stack.Aggregate(text, new Func<string, string, string>(XmlDeltas.ApplyDelta));
    }

    /// <summary>Sets the field value.</summary>
    /// <param name="field">The field.</param>
    /// <param name="value">The value.</param>
    public static void SetFieldValue(Field field, string value)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(value, "value");
      Assert.IsTrue(field.ID == FieldIDs.LayoutField || field.ID == FieldIDs.FinalLayoutField, "The field is not a layout/renderings field");
      string text = null;
      bool flag = field.Item.Name == "__Standard Values";
      bool flag2 = field.ID == FieldIDs.LayoutField;
      Field field2;
      if (flag && flag2)
      {
        field2 = null;
      }
      else if (flag)
      {
        field2 = field.Item.Fields[FieldIDs.LayoutField];
      }
      else if (flag2)
      {
        TemplateItem template = field.Item.Template;
        field2 = ((template != null && template.StandardValues != null) ? template.StandardValues.Fields[FieldIDs.FinalLayoutField] : null);
      }
      else
      {
        field2 = field.Item.Fields[FieldIDs.LayoutField];
      }
      if (field2 != null)
      {
        text = LayoutField.GetFieldValue(field2);
      }
      if (XmlUtil.XmlStringsAreEqual(value, text))
      {
        field.Reset();
        return;
      }
      if (!string.IsNullOrWhiteSpace(text))
      {
        field.Value = XmlDeltas.GetDelta(value, text);
        return;
      }
      field.Value = value;
    }

    /// <summary>
    /// Sets the field value.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="value">The value.</param>
    /// <param name="baseValue">The base value.</param>
    public static void SetFieldValue(Field field, string value, string baseValue)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(value, "value");
      Assert.ArgumentNotNull(baseValue, "baseValue");
      Assert.IsTrue(field.ID == FieldIDs.LayoutField || field.ID == FieldIDs.FinalLayoutField, "The field is not a layout/renderings field");
      if (XmlUtil.XmlStringsAreEqual(value, baseValue))
      {
        field.Reset();
        return;
      }
      string text;
      if (!string.IsNullOrWhiteSpace(baseValue))
      {
        text = XmlDeltas.GetDelta(value, baseValue);
      }
      else
      {
        text = value;
      }
      if (!XmlUtil.XmlStringsAreEqual(XmlDeltas.ApplyDelta(baseValue, field.Value), XmlDeltas.ApplyDelta(baseValue, text)))
      {
        field.Value = text;
      }
    }

    /// <summary>Gets the device node.</summary>
    /// <param name="device">Device.</param>
    /// <returns>The device node.</returns>
    /// <contract>
    ///   <requires name="device" condition="none" />
    /// </contract>
    public System.Xml.XmlNode GetDeviceNode(DeviceItem device)
    {
      if (device != null)
      {
        return this.Data.DocumentElement.SelectSingleNode("d[@id='" + device.ID + "']");
      }
      return null;
    }

    /// <summary>Gets the layout ID.</summary>
    /// <param name="device">Device.</param>
    /// <returns>The layout ID.</returns>
    /// <contract>
    ///   <requires name="device" condition="none" />
    /// </contract>
    public ID GetLayoutID(DeviceItem device)
    {
      Assert.ArgumentNotNull(device, "device");
      System.Xml.XmlNode deviceNode = this.GetDeviceNode(device);
      if (deviceNode != null)
      {
        return LayoutField.ExtractLayoutID(deviceNode);
      }
      return ID.Null;
    }

    /// <summary>Gets the Rendering references for a device.</summary>
    /// <param name="device">Device.</param>
    /// <returns>The references.</returns>
    /// <contract>
    ///   <requires name="device" condition="none" />
    /// </contract>
    public RenderingReference[] GetReferences(DeviceItem device)
    {
      Assert.ArgumentNotNull(device, "device");
      System.Xml.XmlNode deviceNode = this.GetDeviceNode(device);
      if (deviceNode != null)
      {
        return LayoutField.ExtractReferences(deviceNode, base.InnerField.Language, base.InnerField.Database);
      }
      return null;
    }

    /// <summary>Relinks the specified item.</summary>
    /// <param name="itemLink">The item link.</param>
    /// <param name="newLink">The new link.</param>
    public override void Relink(ItemLink itemLink, Item newLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(newLink, "newLink");
      string value = base.Value;
      if (string.IsNullOrEmpty(value))
      {
        return;
      }
      LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
      ArrayList devices = layoutDefinition.Devices;
      if (devices == null)
      {
        return;
      }
      string b = itemLink.TargetItemID.ToString();
      string text = newLink.ID.ToString();
      for (int i = devices.Count - 1; i >= 0; i--)
      {
        DeviceDefinition deviceDefinition = devices[i] as DeviceDefinition;
        if (deviceDefinition != null)
        {
          if (deviceDefinition.ID == b)
          {
            deviceDefinition.ID = text;
          }
          else if (deviceDefinition.Layout == b)
          {
            deviceDefinition.Layout = text;
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
                  placeholderDefinition.MetaDataItemId = newLink.Paths.FullPath;
                  flag = true;
                }
              }
              if (flag)
              {
                goto IL_2B2;
              }
            }
            if (deviceDefinition.Renderings != null)
            {
              for (int k = deviceDefinition.Renderings.Count - 1; k >= 0; k--)
              {
                RenderingDefinition renderingDefinition = deviceDefinition.Renderings[k] as RenderingDefinition;
                if (renderingDefinition != null)
                {
                  if (renderingDefinition.ItemID == b)
                  {
                    renderingDefinition.ItemID = text;
                  }
                  if (renderingDefinition.Datasource == b)
                  {
                    renderingDefinition.Datasource = text;
                  }
                  if (renderingDefinition.Datasource == itemLink.TargetPath)
                  {
                    renderingDefinition.Datasource = newLink.Paths.FullPath;
                  }
                  if (!string.IsNullOrEmpty(renderingDefinition.Parameters))
                  {
                    Item item = base.InnerField.Database.GetItem(renderingDefinition.ItemID);
                    if (item == null)
                    {
                      goto IL_2A4;
                    }
                    RenderingParametersFieldCollection parametersFields = this.GetParametersFields(item, renderingDefinition.Parameters);
                    foreach (CustomField current in parametersFields.Values)
                    {
                      if (!string.IsNullOrEmpty(current.Value))
                      {
                        current.Relink(itemLink, newLink);
                      }
                    }
                    renderingDefinition.Parameters = parametersFields.GetParameters().ToString();
                  }
                  if (renderingDefinition.Rules != null)
                  {
                    RulesField rulesField = new RulesField(base.InnerField, renderingDefinition.Rules.ToString());
                    rulesField.Relink(itemLink, newLink);
                    renderingDefinition.Rules = XElement.Parse(rulesField.Value);
                  }
                }
                IL_2A4:;
              }
            }
          }
        }
        IL_2B2:;
      }
      base.Value = layoutDefinition.ToXml();
    }

    /// <summary>Removes the link.</summary>
    /// <param name="itemLink">The item link.</param>
    public override void RemoveLink(ItemLink itemLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      string value = base.Value;
      if (string.IsNullOrEmpty(value))
      {
        return;
      }
      LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
      ArrayList devices = layoutDefinition.Devices;
      if (devices == null)
      {
        return;
      }
      string b = itemLink.TargetItemID.ToString();
      for (int i = devices.Count - 1; i >= 0; i--)
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
            deviceDefinition.Layout = null;
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
                goto IL_294;
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
                    if (item == null)
                    {
                      goto IL_286;
                    }
                    RenderingParametersFieldCollection parametersFields = this.GetParametersFields(item, renderingDefinition.Parameters);
                    foreach (CustomField current in parametersFields.Values)
                    {
                      if (!string.IsNullOrEmpty(current.Value))
                      {
                        current.RemoveLink(itemLink);
                      }
                    }
                    renderingDefinition.Parameters = parametersFields.GetParameters().ToString();
                  }
                  if (renderingDefinition.Rules != null)
                  {
                    RulesField rulesField = new RulesField(base.InnerField, renderingDefinition.Rules.ToString());
                    rulesField.RemoveLink(itemLink);
                    renderingDefinition.Rules = XElement.Parse(rulesField.Value);
                  }
                }
                IL_286:;
              }
            }
          }
        }
        IL_294:;
      }
      base.Value = layoutDefinition.ToXml();
    }

    /// <summary>Validates the links.</summary>
    /// <param name="result">The result.</param>
    public override void ValidateLinks(LinksValidationResult result)
    {
      Assert.ArgumentNotNull(result, "result");
      string value = base.Value;
      if (string.IsNullOrEmpty(value))
      {
        return;
      }
      LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
      ArrayList devices = layoutDefinition.Devices;
      if (devices == null)
      {
        return;
      }
      foreach (DeviceDefinition deviceDefinition in devices)
      {
        if (!string.IsNullOrEmpty(deviceDefinition.ID))
        {
          Item item = base.InnerField.Database.GetItem(deviceDefinition.ID);
          if (item != null)
          {
            result.AddValidLink(item, deviceDefinition.ID);
          }
          else
          {
            result.AddBrokenLink(deviceDefinition.ID);
          }
        }
        if (!string.IsNullOrEmpty(deviceDefinition.Layout))
        {
          Item item2 = base.InnerField.Database.GetItem(deviceDefinition.Layout);
          if (item2 != null)
          {
            result.AddValidLink(item2, deviceDefinition.Layout);
          }
          else
          {
            result.AddBrokenLink(deviceDefinition.Layout);
          }
        }
        this.ValidatePlaceholderSettings(result, deviceDefinition);
        if (deviceDefinition.Renderings != null)
        {
          foreach (RenderingDefinition renderingDefinition in deviceDefinition.Renderings)
          {
            if (renderingDefinition.ItemID != null)
            {
              Item item3 = base.InnerField.Database.GetItem(renderingDefinition.ItemID);
              if (item3 != null)
              {
                result.AddValidLink(item3, renderingDefinition.ItemID);
              }
              else
              {
                result.AddBrokenLink(renderingDefinition.ItemID);
              }
              string datasource = renderingDefinition.Datasource;
              if (!string.IsNullOrEmpty(datasource))
              {
                using (new ContextItemSwitcher(base.InnerField.Item))
                {
                  ResolveRenderingDatasourceArgs resolveRenderingDatasourceArgs = new ResolveRenderingDatasourceArgs(datasource);
                  CorePipeline.Run("resolveRenderingDatasource", resolveRenderingDatasourceArgs, false);
                  datasource = resolveRenderingDatasourceArgs.Datasource;
                }
                Item item4 = base.InnerField.Database.GetItem(datasource);
                if (item4 != null)
                {
                  result.AddValidLink(item4, datasource);
                }
                else if (!datasource.Contains(":"))
                {
                  result.AddBrokenLink(datasource);
                }
              }
              string multiVariateTest = renderingDefinition.MultiVariateTest;
              if (!string.IsNullOrEmpty(multiVariateTest))
              {
                Item item5 = base.InnerField.Database.GetItem(multiVariateTest);
                if (item5 != null)
                {
                  result.AddValidLink(item5, multiVariateTest);
                }
                else
                {
                  result.AddBrokenLink(multiVariateTest);
                }
              }
              string personalizationTest = renderingDefinition.PersonalizationTest;
              if (!string.IsNullOrEmpty(personalizationTest))
              {
                Item item6 = base.InnerField.Database.GetItem(personalizationTest);
                if (item6 != null)
                {
                  result.AddValidLink(item6, personalizationTest);
                }
                else
                {
                  result.AddBrokenLink(personalizationTest);
                }
              }
              if (item3 != null && !string.IsNullOrEmpty(renderingDefinition.Parameters))
              {
                RenderingParametersFieldCollection parametersFields = this.GetParametersFields(item3, renderingDefinition.Parameters);
                foreach (CustomField current in parametersFields.Values)
                {
                  current.ValidateLinks(result);
                }
              }
              if (renderingDefinition.Rules != null)
              {
                RulesField rulesField = new RulesField(base.InnerField, renderingDefinition.Rules.ToString());
                rulesField.ValidateLinks(result);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Fallback method that used in case <code>getLayoutSourceFields</code> pipeline is not defined.
    /// </summary>
    /// <param name="field">The field to get value for.</param>
    /// <returns>List of values to use as a source for field value.</returns>
    [Obsolete("Use GetLayoutSourceFieldsPipeline.Run(GetLayoutSourceFieldsArgs args) method instead.")]
    private static List<string> DoGetFieldValue(Field field)
    {
      Item item = field.Item;
      FieldCollection fields = item.Fields;
      IEnumerable<Lazy<string>> source = new Lazy<string>[]
      {
        new Lazy<string>(() => fields[FieldIDs.FinalLayoutField].GetValue(false, false) ?? fields[FieldIDs.FinalLayoutField].GetInheritedValue(false)),
        new Lazy<string>(() => fields[FieldIDs.LayoutField].GetValue(false, false) ?? fields[FieldIDs.LayoutField].GetInheritedValue(false)),
        new Lazy<string>(() => fields[FieldIDs.FinalLayoutField].GetStandardValue()),
        new Lazy<string>(() => fields[FieldIDs.LayoutField].GetStandardValue())
      };
      bool flag = item.Name == "__Standard Values";
      bool flag2 = field.ID == FieldIDs.LayoutField;
      if (flag && flag2)
      {
        source = source.Skip(3);
      }
      else if (flag)
      {
        source = source.Skip(2);
      }
      else if (flag2)
      {
        source = source.Skip(1);
      }
      return (from x in source
        select x.Value).ToList<string>();
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
      RenderingParametersFieldCollection result;
      RenderingParametersFieldCollection.TryParse(layoutItem, parameters, out result);
      return result;
    }

    /// <summary>Sets the layout hack.</summary>
    /// <param name="value">The value.</param>
    /// <contract>
    /// 	<requires name="value" condition="not null" />
    /// </contract>
    internal void SetLayoutHack(string value)
    {
      Assert.ArgumentNotNull(value, "value");
      System.Xml.XmlNodeList xmlNodeList = this.Data.DocumentElement.SelectNodes("d");
      Assert.IsNotNull(xmlNodeList, "nodes");
      if (xmlNodeList.Count > 0)
      {
        foreach (System.Xml.XmlNode node in xmlNodeList)
        {
          XmlUtil.SetAttribute("l", value, node);
        }
        base.Value = this.Data.OuterXml;
      }
    }

    /// <summary>Gets the actual value of this field.</summary>
    /// <returns>Actual value of this field object</returns>
    protected override string GetValue()
    {
      if (this._hasRuntimeValue)
      {
        return this._runtimeValue;
      }
      return LayoutField.GetFieldValue(this._innerField);
    }

    /// <summary>
    /// Sets the value of this field.
    /// </summary>
    /// <param name="value">Value to set.</param>
    protected override void SetValue(string value)
    {
      Assert.ArgumentNotNull(value, "value");
      if (this._hasRuntimeValue)
      {
        this._runtimeValue = value;
      }
      LayoutField.SetFieldValue(this._innerField, value);
    }

    /// <summary>Validates the placeholder settings.</summary>
    /// <param name="result">The result.</param>
    /// <param name="device">The device.</param>
    protected virtual void ValidatePlaceholderSettings(LinksValidationResult result, DeviceDefinition device)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(device, "device");
      ArrayList placeholders = device.Placeholders;
      if (placeholders != null)
      {
        foreach (PlaceholderDefinition placeholderDefinition in placeholders)
        {
          if (placeholderDefinition != null && !string.IsNullOrEmpty(placeholderDefinition.MetaDataItemId))
          {
            Item item = base.InnerField.Database.GetItem(placeholderDefinition.MetaDataItemId);
            if (item != null)
            {
              result.AddValidLink(item, placeholderDefinition.MetaDataItemId);
            }
            else
            {
              result.AddBrokenLink(placeholderDefinition.MetaDataItemId);
            }
          }
        }
      }
    }

    /// <summary>Loads the data.</summary>
    /// <returns>The data.</returns>
    private System.Xml.XmlDocument LoadData()
    {
      string value = base.Value;
      if (!string.IsNullOrEmpty(value))
      {
        return XmlUtil.LoadXml(value);
      }
      return XmlUtil.LoadXml("<r/>");
    }
  }
}
