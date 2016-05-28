namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEpisodeNumber : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Episodes", "SeasonNumber", c => c.Int(nullable: true));
            AddColumn("dbo.Episodes", "EpisodeNumber", c => c.Int(nullable: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Episodes", "EpisodeNumber");
            DropColumn("dbo.Episodes", "SeasonNumber");
        }
    }
}
