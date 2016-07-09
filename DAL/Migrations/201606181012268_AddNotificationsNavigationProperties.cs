namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNotificationsNavigationProperties : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Notifications", "Episode_Id", "dbo.Episodes");
            DropForeignKey("dbo.Notifications", "Subscription_Id", "dbo.Subscriptions");
            DropIndex("dbo.Notifications", new[] { "Episode_Id" });
            DropIndex("dbo.Notifications", new[] { "Subscription_Id" });
            RenameColumn(table: "dbo.Notifications", name: "Episode_Id", newName: "EpisodeId");
            RenameColumn(table: "dbo.Notifications", name: "Subscription_Id", newName: "SubscriptionId");
            AlterColumn("dbo.Notifications", "EpisodeId", c => c.Int(nullable: false));
            AlterColumn("dbo.Notifications", "SubscriptionId", c => c.Int(nullable: false));
            CreateIndex("dbo.Notifications", "SubscriptionId");
            CreateIndex("dbo.Notifications", "EpisodeId");
            AddForeignKey("dbo.Notifications", "EpisodeId", "dbo.Episodes", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Notifications", "SubscriptionId", "dbo.Subscriptions", "Id", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Notifications", "SubscriptionId", "dbo.Subscriptions");
            DropForeignKey("dbo.Notifications", "EpisodeId", "dbo.Episodes");
            DropIndex("dbo.Notifications", new[] { "EpisodeId" });
            DropIndex("dbo.Notifications", new[] { "SubscriptionId" });
            AlterColumn("dbo.Notifications", "SubscriptionId", c => c.Int());
            AlterColumn("dbo.Notifications", "EpisodeId", c => c.Int());
            RenameColumn(table: "dbo.Notifications", name: "SubscriptionId", newName: "Subscription_Id");
            RenameColumn(table: "dbo.Notifications", name: "EpisodeId", newName: "Episode_Id");
            CreateIndex("dbo.Notifications", "Subscription_Id");
            CreateIndex("dbo.Notifications", "Episode_Id");
            AddForeignKey("dbo.Notifications", "Subscription_Id", "dbo.Subscriptions", "Id");
            AddForeignKey("dbo.Notifications", "Episode_Id", "dbo.Episodes", "Id");
        }
    }
}
