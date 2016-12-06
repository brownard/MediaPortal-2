<?xml version="1.0" encoding="utf-8"?>
<Image
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  x:Name="MediaItemCover"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
  Stretch="Uniform">
  
  <Image.Resources>
    <Model x:Key="FanArtBackgroundModel" Id="8F42C8E9-E8A3-475C-A50A-99E1E644DC91"/>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Image.Resources>
  
  <Image.Source>
    <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                    Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;512;512}"
                    FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" />
  </Image.Source>

</Image>
