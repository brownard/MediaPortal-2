<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emufanart="clr-namespace:Emulators.Fanart;assembly=Emulators"
  x:Name="MediaItemDetails"
  Margin="-20,10,-15,15">

  <Grid.Resources>
    <emufanart:GameImageSourceConverter x:Key="GameImageSourceConverter"/>
  </Grid.Resources>

  <!-- SHADOW -->
  <Control Style="{ThemeResource Shadow_35_22}" IsVisible="{Binding ElementName=thumb, Path=HasImage}" />
  <!-- SHADOW -->

  <Grid>
    <Rectangle x:Name="mask" Fill="Black" RadiusY="15" RadiusX="15" VerticalAlignment="Center" HorizontalAlignment="Center"/>
    <Grid.OpacityMask>
      <VisualBrush Visual="{Binding ElementName=mask}"/>
    </Grid.OpacityMask>
    <Image x:Name="thumb" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="UniformToFill" Margin="0,0,0,0" FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
           Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=MediaItem, Converter={StaticResource GameImageSourceConverter}, ConverterParameter=FanArt;810;560}">
    </Image>
  </Grid>
</Grid>


<!-- Darkened band -->
<!--Rectangle Grid.Row="1" Grid.Column="0" Fill="#FF000000" Opacity="0.749"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,0,0"
             IsVisible="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SelectedItem,Converter={StaticResource ReferenceToBoolConverter}}"/-->

<!-- Item label -->
<!--Label Grid.Row="1" Grid.Column="0" x:Name="ItemLabel" HorizontalAlignment="Center" VerticalAlignment="Center"  Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
         Color="{ThemeResource TextColor}"/-->


