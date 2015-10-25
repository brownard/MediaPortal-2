<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
  Grid.Column="2" Grid.Row="2" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
  DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="350"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="MediaItemName" Wrap="True" Grid.Column="0" Grid.Row="0" Margin="-10,-15,0,0"
    HorizontalAlignment="Stretch"
    Content="{Binding SimpleTitle}"
    Color="{ThemeResource TextColor}"
    FontFamily="DefaultBold"/>

  <Label x:Name="MediaItemDescription" MaxHeight="100" Wrap="True" Grid.Column="0" Grid.Row="1"
        Scroll="Auto" ScrollSpeed="5" ScrollDelay="5" HorizontalAlignment="Stretch"
        Content="{Binding [Emulators.Description]}"
        Color="{ThemeResource TextColor}" Margin="-10,0,5,25"/>

  <StackPanel Grid.Column="1" Grid.Row="1" Margin="10,0,-20,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="0" HorizontalAlignment="Left"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <emulators:GameAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="340"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="Genre"  Grid.Column="0" Grid.Row="1" Margin="0,0,-10,0"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}" />
          </Grid>
        </ControlTemplate>
      </emulators:GameAspectWrapper.Template>
    </emulators:GameAspectWrapper>
    
    <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <emulators:GameAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="Platform" Grid.Column="0" Grid.Row="2"
                    Content="{Binding Platform}" Color="{ThemeResource TextColor}"/>

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Grid.Column="0" Grid.Row="4" Margin="0,6,0,5">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                           Maximum="10"
                                           Value="{Binding Rating}">
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
            </StackPanel>

          </Grid>
        </ControlTemplate>
      </emulators:GameAspectWrapper.Template>
    </emulators:GameAspectWrapper>

  </StackPanel>

</Grid>
