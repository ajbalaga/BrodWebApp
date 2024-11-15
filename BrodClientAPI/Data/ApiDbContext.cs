using BrodClientAPI.Models;
using MongoDB.Driver;
using MongoDB.Bson;


namespace BrodClientAPI.Data
{
    public class ApiDbContext
    {
        private readonly IMongoDatabase _database;

        public ApiDbContext(IMongoClient client)
        {
            try
            {
                _database = client.GetDatabase("BrodClientDB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to MongoDB: {ex.Message}");
                throw;
            }
        }

        public IMongoCollection<User> User => _database.GetCollection<User>("User");
        public IMongoCollection<Services> Services => _database.GetCollection<Services>("Services");
        //public IMongoCollection<Reviews> Reviews => _database.GetCollection<Reviews>("Reviews");
        public IMongoCollection<Jobs> Jobs => _database.GetCollection<Jobs>("Jobs");
        public IMongoCollection<Rating> Rating => _database.GetCollection<Rating>("Rating");
        public IMongoCollection<Notification> Notification => _database.GetCollection<Notification>("Notification");
        public IMongoCollection<Messages> Messages => _database.GetCollection<Messages>("Messages");
        public IMongoCollection<OTPSMS> OtpSMS => _database.GetCollection<OTPSMS>("OtpSMS");
        public IMongoCollection<OTPEMAIL> OtpEmail => _database.GetCollection<OTPEMAIL>("OtpEmail");
        public IMongoCollection<ClientMessage> ClientMessage => _database.GetCollection<ClientMessage>("ClientMessage");
        public IMongoCollection<TradieMessage> TradieMessage => _database.GetCollection<TradieMessage>("TradieMessage");

        public void Initialize()
        {
            try
            {
                // Create an index on the Username field
                User.Indexes.CreateOne(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u._id)));
                Services.Indexes.CreateOne(new CreateIndexModel<Services>(Builders<Services>.IndexKeys.Ascending(u => u._id)));
                //Reviews.Indexes.CreateOne(new CreateIndexModel<Reviews>(Builders<Reviews>.IndexKeys.Ascending(u => u._id)));
                Jobs.Indexes.CreateOne(new CreateIndexModel<Jobs>(Builders<Jobs>.IndexKeys.Ascending(u => u._id)));
                Rating.Indexes.CreateOne(new CreateIndexModel<Rating>(Builders<Rating>.IndexKeys.Ascending(u => u._id)));
                Notification.Indexes.CreateOne(new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending(u => u._id)));
                Messages.Indexes.CreateOne(new CreateIndexModel<Messages>(Builders<Messages>.IndexKeys.Ascending(u => u._id)));
                OtpSMS.Indexes.CreateOne(new CreateIndexModel<OTPSMS>(Builders<OTPSMS>.IndexKeys.Ascending(u => u._id)));
                OtpEmail.Indexes.CreateOne(new CreateIndexModel<OTPEMAIL>(Builders<OTPEMAIL>.IndexKeys.Ascending(u => u._id)));
                ClientMessage.Indexes.CreateOne(new CreateIndexModel<ClientMessage>(Builders<ClientMessage>.IndexKeys.Ascending(u => u._id)));
                TradieMessage.Indexes.CreateOne(new CreateIndexModel<TradieMessage>(Builders<TradieMessage>.IndexKeys.Ascending(u => u._id)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating indexes: {ex.Message}");
                throw;
            }
        }
    }
}
