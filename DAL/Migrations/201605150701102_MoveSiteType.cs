namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MoveSiteType : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Episodes", "SiteType_Id", "dbo.SiteTypes");
            DropIndex("dbo.Episodes", new[] { "SiteType_Id" });
            AddColumn("dbo.Shows", "SiteTypeId", c => c.Int(nullable: false));
            Sql("UPDATE dbo.Shows SET SiteTypeId = (SELECT Id FROM dbo.SiteTypes WHERE name = 'lostfilm')");
            CreateIndex("dbo.Shows", "SiteTypeId");
            AddForeignKey("dbo.Shows", "SiteTypeId", "dbo.SiteTypes", "Id", cascadeDelete: true);
            DropColumn("dbo.Episodes", "SiteType_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Episodes", "SiteType_Id", c => c.Int());
            DropForeignKey("dbo.Shows", "SiteTypeId", "dbo.SiteTypes");
            DropIndex("dbo.Shows", new[] { "SiteTypeId" });
            DropColumn("dbo.Shows", "SiteTypeId");
            CreateIndex("dbo.Episodes", "SiteType_Id");
            AddForeignKey("dbo.Episodes", "SiteType_Id", "dbo.SiteTypes", "Id");
        }
    }
}
