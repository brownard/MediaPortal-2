<?xml version="1.0" encoding="utf-8"?>
<Grid x:Name="MediaItemMovieDetails"
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <Model x:Key="FanArtBackgroundModel" Id="8F42C8E9-E8A3-475C-A50A-99E1E644DC91"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  
  <Label x:Name="DescriptionLabel" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
         FontSize="{ThemeResource MediumFontSize}" Color="{ThemeResource TextColor}"
         Wrap="True" TextTrimming="WordEllipsis"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"/>

</Grid>