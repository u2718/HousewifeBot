namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddShowSiteIdDescriptionFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Shows", "SiteId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Shows", "SiteId");
        }
    }
}
