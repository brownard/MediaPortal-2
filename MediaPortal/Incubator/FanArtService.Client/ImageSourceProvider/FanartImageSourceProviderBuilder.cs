using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider
{
  /// <summary>
  /// Plugin item builder for <c>FanartImageSourceProviderBuilder</c> plugin items.
  /// </summary>
  public class FanartImageSourceProviderBuilder : IPluginItemBuilder
  {
    public const string FANART_IMAGE_SOURCE_PROVIDER_PATH = "/Fanart/FanartImageSourceProviders";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      return new FanartImageSourceProviderRegistration(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Id);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Noting to do
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    #endregion
  }

  /// <summary>
  /// <see cref="FanartImageSourceProviderRegistration"/> holds extension metadata.
  /// </summary>
  public class FanartImageSourceProviderRegistration
  {
    /// <summary>
    /// Gets the registered type.
    /// </summary>
    public Type ProviderClass { get; private set; }

    /// <summary>
    /// Unique ID of extension.
    /// </summary>
    public Guid Id { get; private set; }

    public FanartImageSourceProviderRegistration(Type type, string providerId)
    {
      ProviderClass = type;
      Id = new Guid(providerId);
    }
  }
}
