using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace Chromatics.Controllers
{
    /// <summary>
    ///     This is a Service Locator to allow for loose coupling of components
    ///     See https://martinfowler.com/articles/injection.html#UsingAServiceLocator for a description.
    /// </summary>
    internal class Locator
    {
        static Locator()
        {
            // Set up IoC container
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            // Register our main applications entry point (Chromatics)
            SimpleIoc.Default.Register<Chromatics>();

            // Register an interface used for writing to the Console/Log/Application
            SimpleIoc.Default.Register<ILogWrite>(() => SimpleIoc.Default.GetInstance<Chromatics>());
        }


        /// <summary>
        ///     Static reference to our main entry point into Chromatics
        /// </summary>
        public static Chromatics ChromaticsInstance => SimpleIoc.Default.GetInstance<Chromatics>();
    }
}