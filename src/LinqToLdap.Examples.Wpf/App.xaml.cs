﻿using System;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using LinqToLdap.Examples.Models;
using LinqToLdap.Examples.Wpf.Helpers;
using LinqToLdap.Examples.Wpf.Messages;
using LinqToLdap.Examples.Wpf.ViewModels;
using LinqToLdap.Examples.Wpf.Views;
using LinqToLdap.Logging;
using LinqToLdap.Mapping;
using SimpleInjector;
using DialogMessage = LinqToLdap.Examples.Wpf.Messages.DialogMessage;

namespace LinqToLdap.Examples.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static Container Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            base.OnStartup(e);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            HandleErrorMessage(new ErrorMessage(args.Exception));
        }

        private void CreateContainer(object sender, StartupEventArgs e)
        {
            var container = new Container();
            container.RegisterSingleton<ILinqToLdapLogger>(() => new SimpleTextLogger(Console.Out));

            container.RegisterSingleton(() => Messenger.Default);
            
            container.RegisterSingleton<ILdapConfiguration>(() =>
            {
                var config = new LdapConfiguration()
                    .MaxPageSizeIs(50)
                    .LogTo(container.GetInstance<ILinqToLdapLogger>());

                // Note the optional parameters on AddMapping.
                // We can perform "late" mapping on certain values, 
                // even for auto and attribute-based mapping.

                config.AddMapping(new OrganizationalUnitMap())
                      .AddMapping(new AttributeClassMap<User>());
                
                // I'm explicitly mapping User here, but I can also let it 
                // get mapped the first time we query for users.
                // This only applies to auto and attribute-based mapping.

                config.ConfigurePooledFactory("directory.utexas.edu")
                      .AuthenticateBy(AuthType.Anonymous)
                      .MinPoolSizeIs(0)
                      .MaxPoolSizeIs(5)
                      .UsePort(389)
                      .ProtocolVersion(3);

                return config;
            });

            container.Register<Func<IDirectoryContext>>(() => container.GetInstance<ILdapConfiguration>().CreateContext);
            container.Register(() => container.GetInstance<Func<IDirectoryContext>>().Invoke());

            Container = container;

            Messenger.Default.Register<ErrorMessage>(this, HandleErrorMessage);
            Messenger.Default.Register<DialogMessage>(this, HandleDialogMessage);

            var view = new MainView(new MainViewModel());
            view.Show();
        }

        private static void HandleErrorMessage(ErrorMessage message)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                ObjectDumper.Write(message.Error, 0, writer);
                MessageBox.Show(sb.ToString(), "LINQ to LDAP WPF Examples Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void HandleDialogMessage(DialogMessage message)
        {
            MessageBoxImage image = message.DialogType == DialogType.Critical
                                        ? MessageBoxImage.Error
                                        : (message.DialogType == DialogType.Error
                                               ? MessageBoxImage.Warning
                                               : MessageBoxImage.Information);

            MessageBox.Show(message.Message, message.Header, MessageBoxButton.OK, image);
        }
    }
}
