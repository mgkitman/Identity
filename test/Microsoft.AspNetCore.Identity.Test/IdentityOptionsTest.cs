// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class IdentityOptionsTest
    {
        [Fact]
        public void VerifyDefaultOptions()
        {
            var options = new IdentityOptions();
            Assert.True(options.Lockout.AllowedForNewUsers);
            Assert.Equal(TimeSpan.FromMinutes(5), options.Lockout.DefaultLockoutTimeSpan);
            Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);

            Assert.True(options.Password.RequireDigit);
            Assert.True(options.Password.RequireLowercase);
            Assert.True(options.Password.RequireNonAlphanumeric);
            Assert.True(options.Password.RequireUppercase);
            Assert.Equal(6, options.Password.RequiredLength);
            Assert.Equal(1, options.Password.RequiredUniqueChars);

            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+", options.User.AllowedUserNameCharacters);
            Assert.False(options.User.RequireUniqueEmail);

            Assert.Equal(ClaimTypes.Role, options.ClaimsIdentity.RoleClaimType);
            Assert.Equal(ClaimTypes.Name, options.ClaimsIdentity.UserNameClaimType);
            Assert.Equal(ClaimTypes.NameIdentifier, options.ClaimsIdentity.UserIdClaimType);
            Assert.Equal("AspNet.Identity.SecurityStamp", options.ClaimsIdentity.SecurityStampClaimType);
        }

        [Fact]
        public void IdentityOptionsFromConfig()
        {
            const string roleClaimType = "rolez";
            const string usernameClaimType = "namez";
            const string useridClaimType = "idz";
            const string securityStampClaimType = "stampz";

            var dic = new Dictionary<string, string>
            {
                {"microsoft:aspnetcore:identity:claimsidentity:roleclaimtype", roleClaimType},
                {"microsoft:aspnetcore:identity:claimsidentity:usernameclaimtype", usernameClaimType},
                {"microsoft:aspnetcore:identity:claimsidentity:useridclaimtype", useridClaimType},
                {"microsoft:aspnetcore:identity:claimsidentity:securitystampclaimtype", securityStampClaimType},
                {"microsoft:aspnetcore:identity:user:requireUniqueEmail", "true"},
                {"microsoft:aspnetcore:identity:password:RequiredLength", "10"},
                {"microsoft:aspnetcore:identity:password:RequiredUniqueChars", "5"},
                {"microsoft:aspnetcore:identity:password:RequireNonAlphanumeric", "false"},
                {"microsoft:aspnetcore:identity:password:RequireUpperCase", "false"},
                {"microsoft:aspnetcore:identity:password:RequireDigit", "false"},
                {"microsoft:aspnetcore:identity:password:RequireLowerCase", "false"},
                {"microsoft:aspnetcore:identity:lockout:AllowedForNewUsers", "FALSe"},
                {"microsoft:aspnetcore:identity:lockout:MaxFailedAccessAttempts", "1000"}
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(dic);
            var config = builder.Build();
            Assert.Equal(roleClaimType, config["microsoft:aspnetcore:identity:claimsidentity:roleclaimtype"]);

            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IConfigureOptions<IdentityOptions>, ConfigureDefaults<IdentityOptions>>();
            services.AddIdentity<TestUser,TestRole>();
            var accessor = services.BuildServiceProvider().GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(accessor);
            var options = accessor.Value;
            Assert.Equal(roleClaimType, options.ClaimsIdentity.RoleClaimType);
            Assert.Equal(useridClaimType, options.ClaimsIdentity.UserIdClaimType);
            Assert.Equal(usernameClaimType, options.ClaimsIdentity.UserNameClaimType);
            Assert.Equal(securityStampClaimType, options.ClaimsIdentity.SecurityStampClaimType);
            Assert.True(options.User.RequireUniqueEmail);
            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+", options.User.AllowedUserNameCharacters);
            Assert.False(options.Password.RequireDigit);
            Assert.False(options.Password.RequireLowercase);
            Assert.False(options.Password.RequireNonAlphanumeric);
            Assert.False(options.Password.RequireUppercase);
            Assert.Equal(10, options.Password.RequiredLength);
            Assert.Equal(5, options.Password.RequiredUniqueChars);
            Assert.False(options.Lockout.AllowedForNewUsers);
            Assert.Equal(1000, options.Lockout.MaxFailedAccessAttempts);
        }

        [Fact]
        public void IdentityOptionsActionOverridesConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"microsoft:aspnetcore:identity:user:requireUniqueEmail", "true"},
                {"microsoft:aspnetcore:identity:lockout:MaxFailedAccessAttempts", "1000"}
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(dic);
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(builder.Build())
                .AddSingleton<IConfigureOptions<IdentityOptions>, ConfigureDefaults<IdentityOptions>>();
            services.AddIdentity<TestUser, TestRole>(o => { o.User.RequireUniqueEmail = false; o.Lockout.MaxFailedAccessAttempts++; });
            var accessor = services.BuildServiceProvider().GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(accessor);
            var options = accessor.Value;
            Assert.False(options.User.RequireUniqueEmail);
            Assert.Equal(1001, options.Lockout.MaxFailedAccessAttempts);
        }

        [Fact]
        public void CanCustomizeIdentityOptions()
        {
            var services = new ServiceCollection().Configure<IdentityOptions>(options => options.Password.RequiredLength = -1);
            services.AddIdentity<TestUser,TestRole>();
            var serviceProvider = services.BuildServiceProvider();

            var setup = serviceProvider.GetRequiredService<IConfigureOptions<IdentityOptions>>();
            Assert.NotNull(setup);
            var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(optionsGetter);
            var myOptions = optionsGetter.Value;
            Assert.True(myOptions.Password.RequireLowercase);
            Assert.True(myOptions.Password.RequireDigit);
            Assert.True(myOptions.Password.RequireNonAlphanumeric);
            Assert.True(myOptions.Password.RequireUppercase);
            Assert.Equal(1, myOptions.Password.RequiredUniqueChars);
            Assert.Equal(-1, myOptions.Password.RequiredLength);
        }

        [Fact]
        public void CanSetupIdentityOptions()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>(options => options.User.RequireUniqueEmail = true);
            var serviceProvider = services.BuildServiceProvider();

            var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(optionsGetter);

            var myOptions = optionsGetter.Value;
            Assert.True(myOptions.User.RequireUniqueEmail);
        }
    }
}