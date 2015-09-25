namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Episodes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SiteId = c.Int(nullable: false),
                        Title = c.String(),
                        Date = c.DateTimeOffset(precision: 7),
                        Show_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Shows", t => t.Show_Id)
                .Index(t => t.Show_Id);
            
            CreateTable(
                "dbo.Shows",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(),
                        OriginalTitle = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Notified = c.Boolean(nullable: false),
                        Episode_Id = c.Int(),
                        Subscription_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Episodes", t => t.Episode_Id)
                .ForeignKey("dbo.Subscriptions", t => t.Subscription_Id)
                .Index(t => t.Episode_Id)
                .Index(t => t.Subscription_Id);
            
            CreateTable(
                "dbo.Subscriptions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SubscriptionDate = c.DateTimeOffset(nullable: false, precision: 7),
                        Show_Id = c.Int(),
                        User_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Shows", t => t.Show_Id)
                .ForeignKey("dbo.Users", t => t.User_Id)
                .Index(t => t.Show_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TelegramUserId = c.Int(nullable: false),
                        FirstName = c.String(),
                        LastName = c.String(),
                        Username = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Notifications", "Subscription_Id", "dbo.Subscriptions");
            DropForeignKey("dbo.Subscriptions", "User_Id", "dbo.Users");
            DropForeignKey("dbo.Subscriptions", "Show_Id", "dbo.Shows");
            DropForeignKey("dbo.Notifications", "Episode_Id", "dbo.Episodes");
            DropForeignKey("dbo.Episodes", "Show_Id", "dbo.Shows");
            DropIndex("dbo.Subscriptions", new[] { "User_Id" });
            DropIndex("dbo.Subscriptions", new[] { "Show_Id" });
            DropIndex("dbo.Notifications", new[] { "Subscription_Id" });
            DropIndex("dbo.Notifications", new[] { "Episode_Id" });
            DropIndex("dbo.Episodes", new[] { "Show_Id" });
            DropTable("dbo.Users");
            DropTable("dbo.Subscriptions");
            DropTable("dbo.Notifications");
            DropTable("dbo.Shows");
            DropTable("dbo.Episodes");
        }
    }
}
