<#
.SYNOPSIS
    OpenCode Helper - Production Native GUI for Windows (WPF/PowerShell)
    Combines OpenCode Unlimited reset workflows, ExpressVPN region switching,
    and automatic Quota state probing into an ultra-fast, modern dark dashboard.
#>

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Windows.Forms, System.Web.Extensions

try {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls11 -bor [System.Net.SecurityProtocolType]::Tls
} catch {}

# Application Icon (Base64 OpenCode Icon)
$IconBase64 = "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" +
"qsOz9AqvMi8AAAAASUVORK5CYII="

# Convert Base64 to ImageSource
$iconImageSource = $null
try {
    $iconBytes = [System.Convert]::FromBase64String($IconBase64)
    $ms = New-Object System.IO.MemoryStream(,$iconBytes)
    $decoder = New-Object System.Windows.Media.Imaging.PngBitmapDecoder($ms, [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $iconImageSource = $decoder.Frames[0]
} catch {}

# Window XAML Definition
$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="OpenCode Helper"
        Width="475" Height="440"
        WindowStartupLocation="CenterScreen"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ResizeMode="CanResizeWithGrip">

    <Window.Resources>
        <!-- Modern Dark ComboBox ToggleButton Template -->
        <ControlTemplate x:Key="DarkComboBoxToggleButton" TargetType="ToggleButton">
            <Border Name="Border" CornerRadius="4" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                <Path Name="Arrow" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,10,0" Fill="#94a3b8" Data="M 0 0 L 4 4 L 8 0 Z"/>
            </Border>
            <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter TargetName="Border" Property="Background" Value="#1a1f32"/>
                    <Setter TargetName="Border" Property="BorderBrush" Value="#384566"/>
                    <Setter TargetName="Arrow" Property="Fill" Value="#cbd5e1"/>
                </Trigger>
                <Trigger Property="IsChecked" Value="True">
                    <Setter TargetName="Border" Property="Background" Value="#1a1f32"/>
                    <Setter TargetName="Border" Property="BorderBrush" Value="#3b82f6"/>
                </Trigger>
            </ControlTemplate.Triggers>
        </ControlTemplate>

        <!-- Modern Dark ComboBox TextBox Template -->
        <ControlTemplate x:Key="DarkComboBoxTextBox" TargetType="TextBox">
            <Border Name="Border" Background="Transparent">
                <ScrollViewer x:Name="PART_ContentHost" VerticalAlignment="Center"/>
            </Border>
        </ControlTemplate>

        <!-- Modern Dark ComboBox Style -->
        <Style TargetType="ComboBox">
            <Setter Property="SnapsToDevicePixels" Value="True"/>
            <Setter Property="OverridesDefaultStyle" Value="True"/>
            <Setter Property="ScrollViewer.HorizontalScrollBarVisibility" Value="Auto"/>
            <Setter Property="ScrollViewer.VerticalScrollBarVisibility" Value="Auto"/>
            <Setter Property="ScrollViewer.CanContentScroll" Value="True"/>
            <Setter Property="Foreground" Value="#f1f5f9"/>
            <Setter Property="Background" Value="#10131f"/>
            <Setter Property="BorderBrush" Value="#22293e"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Padding" Value="8,4,8,4"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="ComboBox">
                        <Grid>
                            <ToggleButton Name="ToggleButton"
                                          Template="{StaticResource DarkComboBoxToggleButton}"
                                          Grid.Column="2" Focusable="false"
                                          IsChecked="{Binding Path=IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}"
                                          ClickMode="Press"
                                          Background="{TemplateBinding Background}"
                                          BorderBrush="{TemplateBinding BorderBrush}"
                                          BorderThickness="{TemplateBinding BorderThickness}"/>
                            <ContentPresenter Name="ContentSite" IsHitTestVisible="False"
                                              Content="{TemplateBinding SelectionBoxItem}"
                                              ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                              VerticalAlignment="Center" HorizontalAlignment="Left"
                                              Margin="10,0,24,0"/>
                            <TextBox x:Name="PART_EditableTextBox"
                                     Style="{x:Null}"
                                     Template="{StaticResource DarkComboBoxTextBox}"
                                     HorizontalAlignment="Left" VerticalAlignment="Center"
                                     Margin="10,0,24,0" Focusable="True"
                                     Background="Transparent" Foreground="#f1f5f9"
                                     Visibility="Hidden" IsReadOnly="{TemplateBinding IsReadOnly}"/>
                            <Popup Name="Popup" Placement="Bottom"
                                   IsOpen="{TemplateBinding IsDropDownOpen}"
                                   AllowsTransparency="True" Focusable="False"
                                   PopupAnimation="Slide">
                                <Grid Name="DropDown"
                                      SnapsToDevicePixels="True"
                                      MinWidth="{TemplateBinding ActualWidth}"
                                      MaxHeight="{TemplateBinding MaxDropDownHeight}">
                                    <Border x:Name="DropDownBorder" Background="#0d101a"
                                            BorderThickness="1" BorderBrush="#2d3754" CornerRadius="4"
                                            Margin="0,2,0,4">
                                        <ScrollViewer Margin="2" SnapsToDevicePixels="True">
                                            <StackPanel IsItemsHost="True"/>
                                        </ScrollViewer>
                                    </Border>
                                </Grid>
                            </Popup>
                        </Grid>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsEditable" Value="True">
                                <Setter TargetName="PART_EditableTextBox" Property="Visibility" Value="Visible"/>
                                <Setter TargetName="ContentSite" Property="Visibility" Value="Hidden"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style TargetType="ComboBoxItem">
            <Setter Property="SnapsToDevicePixels" Value="True"/>
            <Setter Property="OverridesDefaultStyle" Value="True"/>
            <Setter Property="Padding" Value="8,5,8,5"/>
            <Setter Property="Foreground" Value="#cbd5e1"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="ComboBoxItem">
                        <Border Name="Border" Padding="{TemplateBinding Padding}" Background="{TemplateBinding Background}" CornerRadius="3">
                            <ContentPresenter/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsHighlighted" Value="True">
                                <Setter TargetName="Border" Property="Background" Value="#1e263d"/>
                                <Setter Property="Foreground" Value="#38bdf8"/>
                            </Trigger>
                            <Trigger Property="IsSelected" Value="True">
                                <Setter TargetName="Border" Property="Background" Value="#2563eb"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Window Control Buttons (Min/Close) -->
        <Style x:Key="TitleBtn" TargetType="Button">
            <Setter Property="Width" Value="26"/>
            <Setter Property="Height" Value="24"/>
            <Setter Property="FontSize" Value="9.5"/>
            <Setter Property="Foreground" Value="#94a3b8"/>
            <Setter Property="Margin" Value="2,0,0,0"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="3" Background="Transparent">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#22293e"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="CloseBtn" TargetType="Button" BasedOn="{StaticResource TitleBtn}">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="3" Background="Transparent">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#dc2626"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Refined Titlebar Check Capsule Button -->
        <Style x:Key="CheckBtn" TargetType="Button">
            <Setter Property="Height" Value="23"/>
            <Setter Property="Margin" Value="0,0,8,0"/>
            <Setter Property="Foreground" Value="#cbd5e1"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="11" Background="#151928" BorderBrush="#2b354d" BorderThickness="1" Padding="8,2,9,2">
                            <StackPanel Orientation="Horizontal" VerticalAlignment="Center" HorizontalAlignment="Center">
                                <Path x:Name="icon" Width="11" Height="11" Stretch="Uniform" Fill="{TemplateBinding Foreground}" Margin="0,0,5,0" VerticalAlignment="Center"
                                      Data="M 12 4 V 1 L 8 5 L 12 9 V 6 C 15.31 6 18 8.69 18 12 C 18 13.01 17.75 13.97 17.3 14.8 L 18.8 16.3 C 19.55 15.05 20 13.58 20 12 C 20 7.58 16.42 4 12 4 Z M 12 18 C 8.69 18 6 15.31 6 12 C 6 10.99 6.25 10.03 6.7 9.2 L 5.2 7.7 C 4.45 8.95 4 10.42 4 12 C 4 16.42 7.58 20 12 20 V 23 L 16 19 L 12 15 V 18 Z" />
                                <TextBlock x:Name="txt" Text="Check" FontSize="9.5" FontWeight="SemiBold" Foreground="{TemplateBinding Foreground}" VerticalAlignment="Center"/>
                            </StackPanel>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#1f283e"/>
                                <Setter TargetName="bdr" Property="BorderBrush" Value="#38bdf8"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#161d2f"/>
                                <Setter TargetName="bdr" Property="BorderBrush" Value="#0284c7"/>
                                <Setter TargetName="bdr" Property="Opacity" Value="0.85"/>
                            </Trigger>
                            <Trigger Property="IsEnabled" Value="False">
                                <Setter TargetName="bdr" Property="Opacity" Value="0.45"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- General Action Buttons -->
        <Style x:Key="PrimaryBtn" TargetType="Button">
            <Setter Property="Background" Value="#2563eb"/>
            <Setter Property="Foreground" Value="#ffffff"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="9.5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#3b82f6"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="4" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#3b82f6"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Opacity" Value="0.85"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="SuccessBtn" TargetType="Button">
            <Setter Property="Background" Value="#16a34a"/>
            <Setter Property="Foreground" Value="#ffffff"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="9.5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#22c55e"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="4" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#22c55e"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Opacity" Value="0.85"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="DarkBtn" TargetType="Button">
            <Setter Property="Background" Value="#141724"/>
            <Setter Property="Foreground" Value="#cbd5e1"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="9.5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#22293e"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="4" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#1e2438"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Opacity" Value="0.85"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <!-- Main Outer Window Surface with Drop Shadow -->
    <Border CornerRadius="10" BorderBrush="#1d2238" BorderThickness="1" Margin="8">
        <Border.Background>
            <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                <GradientStop Color="#0e101a" Offset="0"/>
                <GradientStop Color="#0a0b12" Offset="1"/>
            </LinearGradientBrush>
        </Border.Background>
        <Border.Effect>
            <DropShadowEffect Color="#000000" BlurRadius="16" ShadowDepth="3" Opacity="0.75"/>
        </Border.Effect>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/> <!-- Titlebar -->
                <RowDefinition Height="*"/>    <!-- Content -->
            </Grid.RowDefinitions>

            <!-- 1. CUSTOM SLEEK TITLEBAR (Draggable) -->
            <Border Name="titleBar" Grid.Row="0" CornerRadius="9,9,0,0" Height="34" Padding="12,0,10,0" BorderBrush="#1a1e32" BorderThickness="0,0,0,1">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                        <GradientStop Color="#141726" Offset="0"/>
                        <GradientStop Color="#0f121e" Offset="1"/>
                    </LinearGradientBrush>
                </Border.Background>

                <Grid VerticalAlignment="Center">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                        <Image Name="titleIcon" Width="16" Height="16" Margin="0,0,7,0" VerticalAlignment="Center"/>
                        <TextBlock Text="OpenCode Helper" FontWeight="Bold" FontSize="11.5" Foreground="#f1f5f9" VerticalAlignment="Center"/>
                    </StackPanel>

                    <!-- Refined Capsule Check Button -->
                    <Button Name="btnCheck" Grid.Column="2" Style="{StaticResource CheckBtn}" VerticalAlignment="Center"/>

                    <!-- Divider -->
                    <Rectangle Grid.Column="3" Width="1" Height="14" Fill="#1e2438" Margin="0,0,6,0" VerticalAlignment="Center"/>

                    <!-- Minimize -->
                    <Button Name="btnMin" Grid.Column="4" Content="—" Style="{StaticResource TitleBtn}" VerticalAlignment="Center"/>

                    <!-- Close -->
                    <Button Name="btnClose" Grid.Column="5" Content="✕" Style="{StaticResource CloseBtn}" VerticalAlignment="Center"/>
                </Grid>
            </Border>

            <!-- 2. CONTENT AREA -->
            <StackPanel Grid.Row="1" Margin="14,10,14,8">
                <!-- HERO STATUS CARD -->
                <Border CornerRadius="5" Background="#121522" BorderBrush="#1d2338" BorderThickness="1" Height="36" Padding="11,0,10,0" Margin="0,0,0,6">
                    <Grid VerticalAlignment="Center">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                            <Path Width="12" Height="12" Stretch="Uniform" Fill="#64748b" Margin="0,0,7,0" VerticalAlignment="Center"
                                  Data="M 12 2 A 10 10 0 1 0 12 22 A 10 10 0 1 0 12 2 Z M 3.93 13 A 8.04 8.04 0 0 1 4 11 H 7.92 C 7.97 11.68 8.07 12.35 8.22 13 H 3.93 Z M 8.22 11 H 4 C 4.09 9.97 4.41 9 4.93 8.16 L 8.35 9.53 C 8.26 10.01 8.22 10.5 8.22 11 Z M 12 4.07 C 12.8 5.16 13.43 6.51 13.82 8 H 10.18 C 10.57 6.51 11.2 5.16 12 4.07 Z M 10.11 11 C 10.11 10.33 10.17 9.66 10.28 9 H 13.72 C 13.83 9.66 13.89 10.33 13.89 11 C 13.89 11.67 13.83 12.34 13.72 13 H 10.28 C 10.17 12.34 10.11 11.67 10.11 11 Z M 12 19.93 C 11.2 18.84 10.57 17.49 10.18 16 H 13.82 C 13.43 17.49 12.8 18.84 12 19.93 Z" />
                            <TextBlock Name="lblIp" Text="Probing network status..." FontFamily="Consolas, Segoe UI" FontSize="9.5" Foreground="#94a3b8" VerticalAlignment="Center" TextTrimming="CharacterEllipsis"/>
                        </StackPanel>

                        <!-- Modern Glowing Status Chip -->
                        <Border Name="badgeQuota" Grid.Column="1" CornerRadius="11" Height="22" Background="#0d1f33" BorderBrush="#0284c7" BorderThickness="1" Padding="9,0,11,0" VerticalAlignment="Center">
                            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                                <Grid Width="10" Height="10" Margin="0,0,6,0" VerticalAlignment="Center">
                                    <Ellipse Name="dotQuotaHalo" Width="10" Height="10" Fill="#7dd3fc" Opacity="0.35" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                    <Ellipse Name="dotQuotaInner" Width="6" Height="6" Fill="#38bdf8" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Grid>
                                <TextBlock Name="lblQuota" Text="Checking" FontSize="9.0" FontWeight="SemiBold" Foreground="#93c5fd" VerticalAlignment="Center"/>
                            </StackPanel>
                        </Border>
                    </Grid>
                </Border>

                <!-- QUICK INSTRUCTIONS COLLAPSIBLE DRAWER -->
                <Border CornerRadius="4" Background="#0e111d" BorderBrush="#191e32" BorderThickness="1" Margin="0,0,0,6" Padding="8,5,8,5">
                    <StackPanel>
                        <Grid Name="guideHeader" Cursor="Hand">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="💡 Workflow: Switch VPN → Clean Reset → Launch OpenCode" FontSize="9.0" Foreground="#60a5fa" VerticalAlignment="Center"/>
                            <TextBlock Name="lblGuideToggle" Grid.Column="1" Text="▾ Details" FontSize="8.5" FontWeight="Bold" Foreground="#475569" VerticalAlignment="Center"/>
                        </Grid>
                        <Border Name="guideBody" Visibility="Collapsed" Margin="0,6,0,2" Padding="2,4,2,0" BorderBrush="#1a2035" BorderThickness="0,1,0,0">
                            <StackPanel>
                                <StackPanel Orientation="Horizontal" Margin="0,1.5">
                                    <TextBlock Text="1." FontSize="9.0" FontWeight="Bold" Foreground="#60a5fa" Width="16"/>
                                    <TextBlock Text="If Quota is Limited → Select VPN &amp; click 'Switch'" FontSize="9.0" Foreground="#cbd5e1"/>
                                </StackPanel>
                                <StackPanel Orientation="Horizontal" Margin="0,1.5">
                                    <TextBlock Text="2." FontSize="9.0" FontWeight="Bold" Foreground="#60a5fa" Width="16"/>
                                    <TextBlock Text="Click '⚡ 1-Click Clean Reset' (Preserves chat history &amp; tabs)" FontSize="9.0" Foreground="#cbd5e1"/>
                                </StackPanel>
                                <StackPanel Orientation="Horizontal" Margin="0,1.5">
                                    <TextBlock Text="3." FontSize="9.0" FontWeight="Bold" Foreground="#60a5fa" Width="16"/>
                                    <TextBlock Text="Click '▶ Launch OpenCode' &amp; reopen your session from list" FontSize="9.0" Foreground="#cbd5e1"/>
                                </StackPanel>
                                <StackPanel Orientation="Horizontal" Margin="0,3,0,0">
                                    <TextBlock Text="•" FontSize="9.0" FontWeight="Bold" Foreground="#94a3b8" Width="16"/>
                                    <TextBlock Text="Use 'Clear Banners' if only an error banner is stuck" FontSize="8.5" FontStyle="Italic" Foreground="#94a3b8"/>
                                </StackPanel>
                            </StackPanel>
                        </Border>
                    </StackPanel>
                </Border>

                <!-- CONTROLS -->
                <StackPanel Margin="0,0,0,6">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="46"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="Path" FontSize="9.5" FontWeight="SemiBold" Foreground="#64748b" VerticalAlignment="Center"/>
                        <ComboBox Name="comboProj" Grid.Column="1" IsEditable="True" Height="26" FontSize="9.5" Margin="0,0,4,0"/>
                        <Button Name="btnAutoProj" Grid.Column="2" Content="Auto" Style="{StaticResource DarkBtn}" Width="46" Height="26" Margin="0,0,4,0" ToolTip="Auto-detect active project from OpenCode"/>
                        <Button Name="btnBrowseProj" Grid.Column="3" Content="Browse" Style="{StaticResource DarkBtn}" Width="60" Height="26" ToolTip="Browse for a project directory"/>
                    </Grid>
                </StackPanel>

                <!-- ACTION DECK -->
                <StackPanel Margin="0,2,0,6">
                    <Button Name="btnReset" Content="⚡ 1-Click Clean Reset" Style="{StaticResource PrimaryBtn}" Height="32" FontSize="10.5" FontWeight="Bold" Margin="0,0,0,5"/>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="8"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Button Name="btnClear" Grid.Column="0" Content="🧹 Clear Banners" Style="{StaticResource DarkBtn}" Height="28" FontSize="9.5"/>
                        <Button Name="btnLaunch" Grid.Column="2" Content="▶ Launch OpenCode" Style="{StaticResource SuccessBtn}" Height="28" FontSize="9.5"/>
                    </Grid>
                </StackPanel>

                <!-- LOG HEADER -->
                <Grid Margin="0,4,0,3">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="ACTIVITY LOG" FontSize="8.5" FontWeight="Bold" Foreground="#475569" VerticalAlignment="Center"/>
                    <Button Name="btnClearLog" Grid.Column="1" Content="Clear" Style="{StaticResource DarkBtn}" Height="18" Padding="6,0" FontSize="8.0"/>
                </Grid>

                <Border CornerRadius="4" Background="#07080f" BorderBrush="#181c2d" BorderThickness="1" Height="105" Padding="5,4,5,4">
                    <TextBox Name="txtLog" Background="Transparent" Foreground="#94a3b8" BorderThickness="0"
                             FontFamily="Consolas, monospace" FontSize="8.5" TextWrapping="Wrap"
                             IsReadOnly="True" VerticalScrollBarVisibility="Auto"/>
                </Border>

                <!-- STATUS FOOTER -->
                <Grid Margin="2,5,2,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Name="lblStatus" Grid.Column="0" Text="● Idle" FontFamily="Consolas" FontSize="8.5" Foreground="#475569" VerticalAlignment="Center"/>
                    <TextBlock Grid.Column="1" Text="v1.2" FontSize="8.0" Foreground="#334155" VerticalAlignment="Center"/>
                </Grid>
            </StackPanel>
        </Grid>
    </Border>
</Window>
"@

$reader = (New-Object System.Xml.XmlNodeReader ([xml]$xaml))
$window = [System.Windows.Markup.XamlReader]::Load($reader)

# Set Application Icon
if ($iconImageSource) {
    $window.Icon = $iconImageSource
    $titleIcon = $window.FindName("titleIcon")
    if ($titleIcon) { $titleIcon.Source = $iconImageSource }
}

# Map UI Elements
$titleBar      = $window.FindName("titleBar")
$btnMin        = $window.FindName("btnMin")
$btnClose       = $window.FindName("btnClose")
$btnCheck      = $window.FindName("btnCheck")
$lblIp         = $window.FindName("lblIp")
$lblQuota      = $window.FindName("lblQuota")
$badgeQuota    = $window.FindName("badgeQuota")
$dotQuotaHalo  = $window.FindName("dotQuotaHalo")
$dotQuotaInner = $window.FindName("dotQuotaInner")
$guideHeader   = $window.FindName("guideHeader")
$guideBody     = $window.FindName("guideBody")
$lblGuideToggle= $window.FindName("lblGuideToggle")
$comboProj     = $window.FindName("comboProj")
$btnAutoProj   = $window.FindName("btnAutoProj")
$btnBrowseProj = $window.FindName("btnBrowseProj")
$btnReset      = $window.FindName("btnReset")
$btnClear      = $window.FindName("btnClear")
$btnLaunch     = $window.FindName("btnLaunch")
$btnClearLog   = $window.FindName("btnClearLog")
$txtLog        = $window.FindName("txtLog")
$lblStatus     = $window.FindName("lblStatus")

if ($guideHeader) {
    $guideHeader.Add_MouseLeftButtonDown({
        if ($guideBody.Visibility -eq [System.Windows.Visibility]::Visible) {
            $guideBody.Visibility = [System.Windows.Visibility]::Collapsed
            $lblGuideToggle.Text = "▾ Details"
            $window.Height = 405
        } else {
            $guideBody.Visibility = [System.Windows.Visibility]::Visible
            $lblGuideToggle.Text = "▴ Hide"
            $window.Height = 470
        }
    })
}

$script:lastKnownIp = $null

# Project Auto-Detection Function
function Refresh-ProjectList {
    param([bool]$LogOutput = $false)

    try {
        $dbPath = "$env:APPDATA\ai.opencode.desktop\drafts.sqlite"
        $activeDir = $null
        $detectedDirs = [System.Collections.Generic.List[string]]::new()

        if (Test-Path $dbPath) {
            $code = @"
using System;
using System.Runtime.InteropServices;
public static class WinSqliteDetector {
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_open_v2(byte[] filename, out IntPtr ppDb, int flags, IntPtr zVfs);
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_prepare_v2(IntPtr db, byte[] zSql, int nByte, out IntPtr ppStmt, IntPtr zTail);
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_step(IntPtr pStmt);
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr sqlite3_column_text(IntPtr pStmt, int iCol);
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_finalize(IntPtr pStmt);
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_close_v2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_close_v2(IntPtr db);
}
"@
            if (-not ([System.Management.Automation.PSTypeName]'WinSqliteDetector').Type) {
                Add-Type -TypeDefinition $code -Language CSharp
            }

            [IntPtr]$db = [IntPtr]::Zero
            $dbBytes = [System.Text.Encoding]::UTF8.GetBytes("$dbPath`0")
            if ([WinSqliteDetector]::sqlite3_open_v2($dbBytes, [ref]$db, 1, [IntPtr]::Zero) -eq 0 -and $db -ne [IntPtr]::Zero) {
                $sql = "SELECT key, value FROM state WHERE name LIKE 'opencode.window%.dat' AND key IN ('tabs.recent', 'tabs.info', 'tabs.closed') ORDER BY updated_at DESC;"
                $sqlBytes = [System.Text.Encoding]::UTF8.GetBytes("$sql`0")
                [IntPtr]$stmt = [IntPtr]::Zero
                if ([WinSqliteDetector]::sqlite3_prepare_v2($db, $sqlBytes, -1, [ref]$stmt, [IntPtr]::Zero) -eq 0 -and $stmt -ne [IntPtr]::Zero) {
                    $activeKey = $null
                    $tabsInfoJson = $null
                    $tabsClosedJson = $null

                    while ([WinSqliteDetector]::sqlite3_step($stmt) -eq 100) {
                        $keyPtr = [WinSqliteDetector]::sqlite3_column_text($stmt, 0)
                        $valPtr = [WinSqliteDetector]::sqlite3_column_text($stmt, 1)
                        $k = [System.Runtime.InteropServices.Marshal]::PtrToStringAnsi($keyPtr)
                        $v = [System.Runtime.InteropServices.Marshal]::PtrToStringAnsi($valPtr)
                        if ($k -eq "tabs.recent" -and -not $activeKey) {
                            try {
                                $parsed = $v | ConvertFrom-Json
                                if ($parsed.key) { $activeKey = $parsed.key }
                            } catch {}
                        } elseif ($k -eq "tabs.info" -and -not $tabsInfoJson) {
                            $tabsInfoJson = $v
                        } elseif ($k -eq "tabs.closed" -and -not $tabsClosedJson) {
                            $tabsClosedJson = $v
                        }
                    }
                    [WinSqliteDetector]::sqlite3_finalize($stmt) | Out-Null
                    [WinSqliteDetector]::sqlite3_close_v2($db) | Out-Null

                    if ($tabsInfoJson) {
                        try {
                            $infoObj = $tabsInfoJson | ConvertFrom-Json
                            if ($activeKey -and $infoObj.$activeKey -and $infoObj.$activeKey.directory) {
                                $act = $infoObj.$activeKey.directory
                                if (Test-Path $act) { $activeDir = $act }
                            }
                            foreach ($prop in $infoObj.PSObject.Properties) {
                                $d = $prop.Value.directory
                                if ($d -and (Test-Path $d) -and -not $detectedDirs.Contains($d)) {
                                    $detectedDirs.Add($d)
                                }
                            }
                        } catch {}
                    }

                    if ($tabsClosedJson) {
                        try {
                            $closedList = $tabsClosedJson | ConvertFrom-Json
                            foreach ($item in $closedList) {
                                if ($item.info -and $item.info.directory) {
                                    $d = $item.info.directory
                                    if ($d -and (Test-Path $d) -and -not $detectedDirs.Contains($d)) {
                                        $detectedDirs.Add($d)
                                    }
                                }
                            }
                        } catch {}
                    }
                } else {
                    [WinSqliteDetector]::sqlite3_close_v2($db) | Out-Null
                }
            }
        }

        $current = $comboProj.Text
        $comboProj.Items.Clear()

        [void]$comboProj.Items.Add("All Workspaces (All Sessions)")

        if ($activeDir -and (Test-Path $activeDir)) {
            [void]$comboProj.Items.Add($activeDir)
        }

        foreach ($d in $detectedDirs) {
            if ((Test-Path $d) -and -not $comboProj.Items.Contains($d)) {
                [void]$comboProj.Items.Add($d)
            }
        }

        if ((Test-Path "D:\trickcal-launcher") -and -not $comboProj.Items.Contains("D:\trickcal-launcher")) {
            [void]$comboProj.Items.Add("D:\trickcal-launcher")
        }
        $devPath = Join-Path $userHome "Projects"
        if ((Test-Path $devPath) -and -not $comboProj.Items.Contains($devPath)) {
            [void]$comboProj.Items.Add($devPath)
        }
        if (-not $comboProj.Items.Contains($userHome)) {
            [void]$comboProj.Items.Add($userHome)
        }

        if ($activeDir -and (Test-Path $activeDir)) {
            $comboProj.Text = $activeDir
            if ($LogOutput) {
                Append-Log "✔ Auto-detected active project: $activeDir"
                Set-Status "● Project: $(Split-Path -Leaf $activeDir)"
            }
        } elseif ($current -and (Test-Path $current)) {
            $comboProj.Text = $current
        } elseif ($comboProj.Items.Count -gt 0) {
            $comboProj.SelectedIndex = 0
        }
    } catch {
        if ($LogOutput) { Append-Log "Project detection note: $($_.Exception.Message)" }
    }
}

$userHome = [Environment]::GetFolderPath("UserProfile")
if (-not $userHome) { $userHome = $env:USERPROFILE }
Refresh-ProjectList $true

$btnAutoProj.Add_Click({
    Refresh-ProjectList $true
})
$comboProj.Add_DropDownOpened({
    Refresh-ProjectList $false
})

# Paths for OpenCode-Unlimited
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
$unlimitedCmdCandidates = @(
    (Join-Path $scriptRoot "OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
    (Join-Path $scriptRoot "OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    (Join-Path $scriptRoot "..\OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
    (Join-Path $scriptRoot "..\OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    "D:\opencode-manager\OpenCode-Unlimited\bin\opencode-unlimited.ps1",
    "D:\opencode-manager\OpenCode-Unlimited\bin\opencode-unlimited.cmd",
    (Join-Path $userHome "Infosoft\OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
    (Join-Path $userHome "Infosoft\OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    (Join-Path $userHome "OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
    (Join-Path $userHome "OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    "opencode-unlimited.ps1",
    "opencode-unlimited.cmd"
)
$script:unlimitedCmd = "opencode-unlimited.cmd"
foreach ($c in $unlimitedCmdCandidates) {
    if (Test-Path $c) { $script:unlimitedCmd = (Resolve-Path $c).Path; break }
}

# Resolve ExpressVPN CLI
$script:expressVpnPath = ""
$vpnCandidates = @(
    "C:\Program Files (x86)\ExpressVPN\services\ExpressVPN.CLI.exe",
    "C:\Program Files\ExpressVPN\services\ExpressVPN.CLI.exe",
    (Join-Path $env:ProgramFiles "ExpressVPN\services\ExpressVPN.CLI.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "ExpressVPN\services\ExpressVPN.CLI.exe")
)
foreach ($v in $vpnCandidates) {
    if (Test-Path $v) { $script:expressVpnPath = $v; break }
}

# Logging Helpers
function Append-Log([string]$message) {
    $time = (Get-Date).ToString("[HH:mm:ss] ")
    $txtLog.AppendText($time + $message + [Environment]::NewLine)
    $txtLog.ScrollToEnd()
}

function Append-RawLog([string]$line) {
    $txtLog.AppendText("  " + $line + [Environment]::NewLine)
    $txtLog.ScrollToEnd()
}

function Set-Status([string]$status) {
    $lblStatus.Text = $status
}

function Update-QuotaBadge([string]$state, [string]$text) {
    $bc = [System.Windows.Media.BrushConverter]::new()
    $lblQuota.Text = $text
    if ($state -eq "ready") {
        $badgeQuota.Background = $bc.ConvertFromString("#06281e")
        $badgeQuota.BorderBrush = $bc.ConvertFromString("#0d7355")
        $dotQuotaHalo.Fill = $bc.ConvertFromString("#34d399")
        $dotQuotaInner.Fill = $bc.ConvertFromString("#10b981")
        $lblQuota.Foreground = $bc.ConvertFromString("#34d399")
    } elseif ($state -eq "limited") {
        $badgeQuota.Background = $bc.ConvertFromString("#2c1219")
        $badgeQuota.BorderBrush = $bc.ConvertFromString("#9f1239")
        $dotQuotaHalo.Fill = $bc.ConvertFromString("#fb7185")
        $dotQuotaInner.Fill = $bc.ConvertFromString("#f43f5e")
        $lblQuota.Foreground = $bc.ConvertFromString("#fda4af")
    } elseif ($state -eq "offline") {
        $badgeQuota.Background = $bc.ConvertFromString("#241217")
        $badgeQuota.BorderBrush = $bc.ConvertFromString("#881337")
        $dotQuotaHalo.Fill = $bc.ConvertFromString("#f87171")
        $dotQuotaInner.Fill = $bc.ConvertFromString("#ef4444")
        $lblQuota.Foreground = $bc.ConvertFromString("#fca5a5")
    } else {
        $badgeQuota.Background = $bc.ConvertFromString("#0d1f33")
        $badgeQuota.BorderBrush = $bc.ConvertFromString("#0284c7")
        $dotQuotaHalo.Fill = $bc.ConvertFromString("#7dd3fc")
        $dotQuotaInner.Fill = $bc.ConvertFromString("#38bdf8")
        $lblQuota.Foreground = $bc.ConvertFromString("#93c5fd")
    }
}

# Window Draggable & Title Buttons
$titleBar.Add_MouseLeftButtonDown({
    $window.DragMove()
})

$btnMin.Add_Click({
    $window.WindowState = [System.Windows.WindowState]::Minimized
})

$btnClose.Add_Click({
    $window.Close()
})

$btnClearLog.Add_Click({
    $txtLog.Clear()
})

# Browse Project Directory
$btnBrowseProj.Add_Click({
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = "Select OpenCode Project Directory"
    $dialog.SelectedPath = $comboProj.Text
    if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $comboProj.Text = $dialog.SelectedPath
        if (-not $comboProj.Items.Contains($dialog.SelectedPath)) {
            [void]$comboProj.Items.Add($dialog.SelectedPath)
        }
    }
})

# Network & Quota Probe
function Check-StatusAsync([bool]$Silent = $false) {
    if (-not $Silent) {
        Append-Log "Probing IP & OpenCode gateway status..."
        Set-Status "● Probing gateway..."
        Update-QuotaBadge "probing" "Checking"
    }

    $client = New-Object System.Net.WebClient
    $client.Headers.Add("User-Agent", "curl/8.0")
    
    $eventHandler = {
        param($sender, $e)
        if ($e.Error -ne $null) {
            if (-not $Silent) {
                $window.Dispatcher.Invoke([Action]{
                    $lblIp.Text = "Offline / Connection Error"
                    Update-QuotaBadge "offline" "Offline"
                    Set-Status "● Awaiting network / VPN..."
                    Append-Log "Network error: $($e.Error.Message)"
                })
            }
            return
        }

        try {
            $jser = New-Object System.Web.Script.Serialization.JavaScriptSerializer
            $data = $jser.DeserializeObject($e.Result)
            $ip = $data["ip"]
            $city = $data["city"]
            $country = $data["country"]
            $ipText = "$ip  •  $city, $country"

            $ipChanged = ($script:lastKnownIp -and $script:lastKnownIp -ne $ip)
            if ($Silent -and -not $ipChanged) {
                return
            }

            if ($ipChanged) {
                $window.Dispatcher.Invoke([Action]{
                    Append-Log "🔄 Connection change detected: $ipText"
                })
            }
            $script:lastKnownIp = $ip

            $isRateLimited = $false
            $quotaDetails = "Ready (Free Tier Available)"

            # Check local spent ledger
            if (-not $isRateLimited) {
                $egressFile = Join-Path $userHome ".config\opencode\unlimited-egress.json"
                if (Test-Path $egressFile) {
                    try {
                        $raw = [System.IO.File]::ReadAllText($egressFile)
                        $egress = $jser.DeserializeObject($raw)
                        $today = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
                        if ($egress["day"] -eq $today -and $egress["addresses"] -ne $null) {
                            $addr = $egress["addresses"]
                            $ipKey = "$ip/32"
                            if ($addr.ContainsKey($ipKey) -and $addr[$ipKey]["spent"] -eq $true) {
                                $isRateLimited = $true
                                $quotaDetails = "Rate-Limited (Recorded in local spent ledger)"
                            }
                        }
                    } catch {}
                }
            }

            # Quota status is determined strictly by the ledger or active session without false-positive poisoning

            $window.Dispatcher.Invoke([Action]{
                $lblIp.Text = $ipText
                if ($isRateLimited) {
                    Update-QuotaBadge "limited" "Rate-Limited"
                    Set-Status "● Awaiting VPN connection..."
                    Append-Log "Network: $ipText"
                    Append-Log "✖ Quota: $quotaDetails"
                    Append-Log "Awaiting VPN connection to acquire a fresh IP..."
                } else {
                    Update-QuotaBadge "ready" "Ready"
                    Set-Status "● Ready (Free Tier Available)"
                    Append-Log "Network: $ipText"
                    Append-Log "✔ Quota: $quotaDetails"
                }
            })
        } catch {
            if (-not $Silent) {
                $window.Dispatcher.Invoke([Action]{
                    $lblIp.Text = "Parse Error"
                    Update-QuotaBadge "offline" "Offline"
                    Set-Status "● Idle"
                    Append-Log "Error processing gateway payload."
                })
            }
        }
    }

    $client.Add_DownloadStringCompleted($eventHandler)
    $client.DownloadStringAsync((New-Object System.Uri("https://ipinfo.io/json")))
}

$btnCheck.Add_Click({
    Check-StatusAsync $false
})

# Subprocess Runner with live UI log streaming
function Invoke-AsyncProcess {
    param([string]$FilePath, [string]$Arguments, [scriptblock]$OnDone)

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = $Arguments
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    $proc.EnableRaisingEvents = $true

    $outEv = Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived -Action {
        if ($EventArgs.Data) {
            $window.Dispatcher.Invoke([Action]{
                Append-RawLog $EventArgs.Data
            })
        }
    }
    $errEv = Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived -Action {
        if ($EventArgs.Data) {
            $window.Dispatcher.Invoke([Action]{
                Append-RawLog $EventArgs.Data
            })
        }
    }
    $exitEv = Register-ObjectEvent -InputObject $proc -EventName Exited -Action {
        Unregister-Event -SourceIdentifier $outEv.Name -ErrorAction SilentlyContinue
        Unregister-Event -SourceIdentifier $errEv.Name -ErrorAction SilentlyContinue
        Unregister-Event -SourceIdentifier $exitEv.Name -ErrorAction SilentlyContinue
        $window.Dispatcher.Invoke([Action]{
            if ($OnDone) { & $OnDone }
        })
    }

    $proc.Start() | Out-Null
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()
}

function Invoke-UnlimitedCommand {
    param([string]$Arguments, [scriptblock]$OnDone)

    $ps1 = if ($script:unlimitedCmd -like "*.ps1") { $script:unlimitedCmd } else { [System.IO.Path]::ChangeExtension($script:unlimitedCmd, ".ps1") }
    if (Test-Path $ps1) {
        Invoke-AsyncProcess -FilePath "powershell.exe" -Arguments "-NoLogo -NoProfile -ExecutionPolicy Bypass -File `"$ps1`" $Arguments" -OnDone $OnDone
    } else {
        Invoke-AsyncProcess -FilePath "cmd.exe" -Arguments "/c `"`"$script:unlimitedCmd`" $Arguments`"" -OnDone $OnDone
    }
}

# 1-Click Clean Reset
$btnReset.Add_Click({
    $proj = $comboProj.Text
    if (-not $proj) { $proj = $userHome }

    Append-Log "════════════════════════════════════"
    Append-Log "🚀 1-Click Clean Reset initiated"
    Append-Log "Target directory: $proj"
    Set-Status "● Closing OpenCode & resetting..."

    # 1. Terminate OpenCode safely without killing self
    Append-Log "Terminating OpenCode instances (safe)..."
    Get-Process | Where-Object { 
        $_.Id -ne $PID -and (
            $_.ProcessName -like "*opencode*" -or
            $_.ProcessName -eq "OpenCode Desktop" -or
            $_.ProcessName -eq "ai.opencode.desktop"
        ) -and $_.ProcessName -notlike "*OpenCodeHelper*" -and $_.ProcessName -notlike "*opencode-manager*"
    } | Stop-Process -Force -ErrorAction SilentlyContinue

    Start-Process -FilePath "taskkill.exe" -ArgumentList "/F /T /IM `"OpenCode Desktop.exe`" /IM `"ai.opencode.desktop.exe`" /IM `"opencode.exe`" /IM `"opencode-cli.exe`"" -WindowStyle Hidden -Wait -ErrorAction SilentlyContinue

    Start-Sleep -Milliseconds 500

    $isAll = ($proj -eq "All Workspaces (All Sessions)" -or $proj -like "All Workspaces*")
    if ($isAll) {
        $targets = @()
        foreach ($item in $comboProj.Items) {
            if ($item -and $item -ne "All Workspaces (All Sessions)" -and (Test-Path $item) -and -not ($targets -contains $item)) {
                $targets += $item
            }
        }
        if ($targets.Count -eq 0) { $targets += $userHome }

        Append-Log "Running opencode-unlimited clear across all sessions..."
        Invoke-UnlimitedCommand "clear" -OnDone {
            Append-Log "Resetting session IDs across $($targets.Count) workspace(s)..."
            $ps1 = if ($script:unlimitedCmd -like "*.ps1") { $script:unlimitedCmd } else { [System.IO.Path]::ChangeExtension($script:unlimitedCmd, ".ps1") }
            foreach ($t in $targets) {
                Append-Log "  -> Resetting workspace: $t"
                if (Test-Path $ps1) {
                    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "$ps1" reset "$t" | ForEach-Object { Append-RawLog $_ }
                } else {
                    & cmd.exe /c "`"`"$script:unlimitedCmd`" reset `"$t`"`"" | ForEach-Object { Append-RawLog $_ }
                }
            }
            Append-Log "✔ Clean reset finished! All chat history preserved."
            Set-Status "● Reset done! Launch OpenCode."
            Check-StatusAsync
        }
    } else {
        Append-Log "Running opencode-unlimited clear..."
        Invoke-UnlimitedCommand "clear" -OnDone {
            Append-Log "Running opencode-unlimited reset `"$proj`"..."
            Invoke-UnlimitedCommand "reset `"$proj`"" -OnDone {
                Append-Log "✔ Clean reset finished! All chat history preserved."
                Set-Status "● Reset done! Launch OpenCode."
                Check-StatusAsync
            }
        }
    }
})

# Clear Banners
$btnClear.Add_Click({
    Append-Log "════════════════════════════════════"
    Append-Log "🧹 Clearing stored error banners..."
    Set-Status "● Stripping banners..."

    Invoke-UnlimitedCommand "clear" -OnDone {
        Append-Log "✔ Banners stripped! Chat sessions untouched."
        Set-Status "● Banners stripped (Tabs safe)."
    }
})

# Launch OpenCode
$btnLaunch.Add_Click({
    Append-Log "════════════════════════════════════"
    Append-Log "▶ Launching OpenCode desktop..."
    Set-Status "● Launching OpenCode..."

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\ai.opencode.desktop\OpenCode.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\opencode\opencode.exe"),
        (Join-Path $env:ProgramFiles "opencode\opencode.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "opencode\opencode.exe"),
        (Join-Path $userHome ".opencode\bin\opencode.exe"),
        "opencode.cmd",
        "opencode.exe"
    )

    $exeToLaunch = ""
    foreach ($cand in $candidates) {
        if (Test-Path $cand) { $exeToLaunch = $cand; break }
    }
    if (-not $exeToLaunch) { $exeToLaunch = "opencode" }

    try {
        $projDir = $comboProj.Text
        $startArgs = @{ FilePath = $exeToLaunch; PassThru = $true }
        if ($projDir -and (Test-Path $projDir)) {
            $startArgs["WorkingDirectory"] = $projDir
            $startArgs["ArgumentList"] = "`"$projDir`""
        }
        $p = Start-Process @startArgs
        Append-Log "✔ OpenCode launched (PID: $($p.Id))$(if ($projDir) { " in $projDir" })"
        Set-Status "● OpenCode running."
    } catch {
        Append-Log "✖ Failed to launch OpenCode directly: $($_.Exception.Message)"
        try {
            Start-Process "cmd.exe" -ArgumentList "/c opencode" -WindowStyle Hidden
            Append-Log "✔ OpenCode launched via system shell."
            Set-Status "● OpenCode running."
        } catch {
            Append-Log "✖ Could not locate or start OpenCode."
            Set-Status "● Launch failed."
        }
    }
})

# Initial load
Append-Log "OpenCode Helper (Windows) initialized. Ready."
Check-StatusAsync $false

# Background VPN connection watcher
$vpnTimer = New-Object System.Windows.Threading.DispatcherTimer
$vpnTimer.Interval = [TimeSpan]::FromSeconds(4)
$vpnTimer.Add_Tick({
    Check-StatusAsync $true
})
$vpnTimer.Start()

[void]$window.ShowDialog()
