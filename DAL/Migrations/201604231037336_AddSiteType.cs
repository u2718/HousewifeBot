namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSiteType : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SiteTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Episodes", "SiteType_Id", c => c.Int());
            CreateIndex("dbo.Episodes", "SiteType_Id");
            AddForeignKey("dbo.Episodes", "SiteType_Id", "dbo.SiteTypes", "Id");
            Sql("INSERT INTO dbo.SiteTypes (title, name) VALUES ('LostFilm.tv', 'lostfilm'), ('NewStudio.tv', 'newstudio')");
            Sql("UPDATE dbo.Episodes SET SiteType_Id = (SELECT Id FROM dbo.SiteTypes WHERE name = 'lostfilm')");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Episodes", "SiteType_Id", "dbo.SiteTypes");
            DropIndex("dbo.Episodes", new[] { "SiteType_Id" });
            DropColumn("dbo.Episodes", "SiteType_Id");
            DropTable("dbo.SiteTypes");
        }
    }
}
