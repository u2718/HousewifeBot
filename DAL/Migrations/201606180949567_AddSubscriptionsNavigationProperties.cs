namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSubscriptionsNavigationProperties : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Subscriptions", "User_Id", "dbo.Users");
            DropForeignKey("dbo.Subscriptions", "Show_Id", "dbo.Shows");
            DropIndex("dbo.Subscriptions", new[] { "Show_Id" });
            DropIndex("dbo.Subscriptions", new[] { "User_Id" });
            RenameColumn(table: "dbo.Subscriptions", name: "User_Id", newName: "UserId");
            RenameColumn(table: "dbo.Subscriptions", name: "Show_Id", newName: "ShowId");
            AlterColumn("dbo.Subscriptions", "ShowId", c => c.Int(nullable: false));
            AlterColumn("dbo.Subscriptions", "UserId", c => c.Int(nullable: false));
            CreateIndex("dbo.Subscriptions", "UserId");
            CreateIndex("dbo.Subscriptions", "ShowId");
            AddForeignKey("dbo.Subscriptions", "UserId", "dbo.Users", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Subscriptions", "ShowId", "dbo.Shows", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Subscriptions", "ShowId", "dbo.Shows");
            DropForeignKey("dbo.Subscriptions", "UserId", "dbo.Users");
            DropIndex("dbo.Subscriptions", new[] { "ShowId" });
            DropIndex("dbo.Subscriptions", new[] { "UserId" });
            AlterColumn("dbo.Subscriptions", "UserId", c => c.Int());
            AlterColumn("dbo.Subscriptions", "ShowId", c => c.Int());
            RenameColumn(table: "dbo.Subscriptions", name: "ShowId", newName: "Show_Id");
            RenameColumn(table: "dbo.Subscriptions", name: "UserId", newName: "User_Id");
            CreateIndex("dbo.Subscriptions", "User_Id");
            CreateIndex("dbo.Subscriptions", "Show_Id");
            AddForeignKey("dbo.Subscriptions", "Show_Id", "dbo.Shows", "Id");
            AddForeignKey("dbo.Subscriptions", "User_Id", "dbo.Users", "Id");
        }
    }
}
