/*
 Copyright (c) 2024 Iamshen . All rights reserved.

 Copyright (c) 2024 HigginsSoft, Alexander Higgins - https://github.com/alexhiggins732/ 

 Copyright (c) 2018, Brock Allen & Dominick Baier. All rights reserved.

 Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information. 
 Source code and license this software can be found 

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.
*/

using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityServerHost.Models.IdentityRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// RSA Key Pairs cho các Client
    /// </summary>
    public DbSet<ClientKeyPair> ClientKeyPairs => Set<ClientKeyPair>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // ClientKeyPair configuration
        builder.Entity<ClientKeyPair>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClientId).IsUnique();
            e.Property(x => x.ClientId).HasMaxLength(200);
            e.Property(x => x.PrivateKey).IsRequired();
            e.Property(x => x.PublicKey).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
        });
    }
}
