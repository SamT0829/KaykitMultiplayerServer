namespace KayKitMultiplayerServer.DBRelated
{
    public interface IQueryEventListener
    {
        void QueryDoneCallback(DBCatagory dbCatagory, int index);
    }
}