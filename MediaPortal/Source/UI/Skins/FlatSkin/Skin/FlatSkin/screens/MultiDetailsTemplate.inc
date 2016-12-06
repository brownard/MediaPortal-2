<?xml version="1.0" encoding="utf-8"?>
<ContentControl
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:navitems="clr-namespace:MediaPortal.UiComponents.Media.Models.Navigation;assembly=Media"
    HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
    Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SelectedItem}">

  <ContentControl.Resources>

    <!--<DataTemplate DataType="{x:Type navitems:MovieItem}">
      <Include Source="screens\MediaItemOthersDetails.inc"/>
    </DataTemplate>

    <DataTemplate DataType="{x:Type navitems:EpisodeItem}">
      <Include Source="screens\MediaItemOthersDetails.inc"/>
    </DataTemplate>-->
    
  </ContentControl.Resources>
  
  <ContentControl.ContentTemplate>
    <DataTemplate>
      <Include Source="screens\MediaItemOthersDetails.inc"/>
    </DataTemplate>
  </ContentControl.ContentTemplate>
  
  <ContentControl.TemplateControl>
    <ContentPresenter x:Name="ContentPresenter" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/>
  </ContentControl.TemplateControl>
</ContentControl>