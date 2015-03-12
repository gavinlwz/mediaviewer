﻿using MediaViewer.Infrastructure.Logging;
using MediaViewer.MediaDatabase.DbCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase
{
    class InitializeDb
    {
        public static void initialize()
        {
            try
            {
                createIndexes();
            }
            catch (Exception e)
            {
                Logger.Log.Error("Error initializing database", e);
            }
        }

        private static void createIndexes()
        {
            using (MediaDbCommands db = new MediaDbCommands())
            {
                var query = new StringBuilder();
                query.Append(db.CreateIndex<BaseMedia>(x => x.Id));
                query.Append(db.CreateIndex<VideoMedia>(x => x.Id));
                query.Append(db.CreateIndex<ImageMedia>(x => x.Id));
                query.Append(db.CreateIndex<Tag>(x => x.Id));

                String result = query.ToString();
              
                db.Db.Database.ExecuteSqlCommand(query.ToString());
                
            }

        }
    }
}
