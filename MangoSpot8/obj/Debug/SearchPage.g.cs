﻿#pragma checksum "C:\Users\Nicholas Bryan Brook\documents\visual studio 2015\Projects\MangoSpot8\MangoSpot8\SearchPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "9E004250D4199727FCE962875401886E"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace MangoSpot8 {
    
    
    public partial class SearchPage : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.TextBox searchBox;
        
        internal System.Windows.Controls.ScrollViewer AlbumScrollViewer;
        
        internal System.Windows.Controls.ItemsControl AlbumItemsControl;
        
        internal System.Windows.Controls.ScrollViewer SongScrollViewer;
        
        internal System.Windows.Controls.ItemsControl SongItemsControl;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/MangoSpot8;component/SearchPage.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.searchBox = ((System.Windows.Controls.TextBox)(this.FindName("searchBox")));
            this.AlbumScrollViewer = ((System.Windows.Controls.ScrollViewer)(this.FindName("AlbumScrollViewer")));
            this.AlbumItemsControl = ((System.Windows.Controls.ItemsControl)(this.FindName("AlbumItemsControl")));
            this.SongScrollViewer = ((System.Windows.Controls.ScrollViewer)(this.FindName("SongScrollViewer")));
            this.SongItemsControl = ((System.Windows.Controls.ItemsControl)(this.FindName("SongItemsControl")));
        }
    }
}

