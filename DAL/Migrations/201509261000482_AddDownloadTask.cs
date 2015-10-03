namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDownloadTask : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DownloadTasks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TorrentUrl = c.String(),
                        DownloadStarted = c.Boolean(nullable: false),
                        Episode_Id = c.Int(),
                        User_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Episodes", t => t.Episode_Id)
                .ForeignKey("dbo.Users", t => t.User_Id)
                .Index(t => t.Episode_Id)
                .Index(t => t.User_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DownloadTasks", "User_Id", "dbo.Users");
            DropForeignKey("dbo.DownloadTasks", "Episode_Id", "dbo.Episodes");
            DropIndex("dbo.DownloadTasks", new[] { "User_Id" });
            DropIndex("dbo.DownloadTasks", new[] { "Episode_Id" });
            DropTable("dbo.DownloadTasks");
        }
    }
}
