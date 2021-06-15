using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Core
{
    [TestFixture]
    [UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
    public class RuntimeStateTests : UmbracoIntegrationTest
    {
        private protected IRuntimeState RuntimeState { get; private set; }

        public override void Configure(IApplicationBuilder app)
        {
            base.Configure(app);

            RuntimeState = Services.GetRequiredService<IRuntimeState>();
        }

        protected override void CustomTestSetup(IUmbracoBuilder builder)
        {
            PackageMigrationPlanCollectionBuilder migrations = builder.PackageMigrationPlans();
            migrations.Clear();
            migrations.Add<TestMigrationPlan>();
        }

        [Test]
        public void GivenPackageMigrationsExist_WhenLatestStateIsRegistered_ThenLevelIsRun()
        {
            // Add the final state to the keyvalue storage
            IKeyValueService keyValueService = Services.GetRequiredService<IKeyValueService>();
            keyValueService.SetValue(
                Constants.Conventions.Migrations.KeyValuePrefix + TestMigrationPlan.TestMigrationPlanName,
                TestMigrationPlan.TestMigrationFinalState.ToString());

            RuntimeState.DetermineRuntimeLevel();

            Assert.AreEqual(RuntimeLevel.Run, RuntimeState.Level);
            Assert.AreEqual(RuntimeLevelReason.Run, RuntimeState.Reason);
        }

        [Test]
        public void GivenPackageMigrationsExist_WhenUnattendedMigrations_ThenLevelIsRun()
        {
            RuntimeState.DetermineRuntimeLevel();

            Assert.AreEqual(RuntimeLevel.Run, RuntimeState.Level);
            Assert.AreEqual(RuntimeLevelReason.UpgradePackageMigrations, RuntimeState.Reason);
        }

        [Test]
        public void GivenPackageMigrationsExist_WhenNotUnattendedMigrations_ThenLevelIsPackageMigrations()
        {
            var unattendedOptions = Services.GetRequiredService<IOptions<UnattendedSettings>>();
            unattendedOptions.Value.PackageMigrationsUnattended = false;

            RuntimeState.DetermineRuntimeLevel();

            Assert.AreEqual(RuntimeLevel.PackageMigrations, RuntimeState.Level);
            Assert.AreEqual(RuntimeLevelReason.UpgradePackageMigrations, RuntimeState.Reason);
        }

        private class TestMigrationPlan : PackageMigrationPlan
        {
            public const string TestMigrationPlanName = "Test";
            public static Guid TestMigrationFinalState => new Guid("BB02C392-4007-4A6C-A550-28BA2FF7E43D");

            public TestMigrationPlan() : base(TestMigrationPlanName)
            {
            }

            protected override void DefinePlan()
            {
                To<TestMigration>(TestMigrationFinalState);
            }
        }

        private class TestMigration : PackageMigrationBase
        {
            public TestMigration(IPackagingService packagingService, IMigrationContext context)
                : base(packagingService, context)
            {
            }

            public override void Migrate()
            {
                ImportPackage.FromEmbeddedResource<TestMigration>().Do();
            }
        }
    }
}
