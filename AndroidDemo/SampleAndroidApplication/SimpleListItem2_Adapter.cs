using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace SampleAndroidApplication
{
    public class SimpleListItem2_Adapter : ArrayAdapter<Tuple<string, TaskItem>>
    {
        Activity context;
        public SimpleListItem2_Adapter(Activity context, IList<Tuple<string, TaskItem>> objects) 
            : base(context, Android.Resource.Id.Text1, objects)
        {
            this.context = context;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);

            var item = GetItem(position);

            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = ((TaskItem)item.Item2).Title;
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = ((TaskItem)item.Item2).CreatedDate.ToString();

            return view;
        }
    }
}