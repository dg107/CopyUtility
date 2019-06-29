using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Utility
{
    public static class CopyLibary
    {
        public static void SetPropertyValue<T>(this T Item, string PropertyName, object Value)
        {
            if (Value == null)
                return;

            try
            {
                switch (PropertyName.ToCharArray()[0])
                {
                    case '{' // A Map to a deeper property
                   :
                        {
                            SetPropertyValuePath(Item, PropertyName, Value);
                            break;
                        }

                    case '#' // Dictionary/Hashset, any object that has a string indexed name value pair
             :
                        {
                            string[] Command = PropertyName.Split(',');
                            Command[0] = Command[0].Substring(1);
                            object Obj = Item.GetType().GetProperties().First(x => x.Name.ToUpper() == Command[0].ToUpper()).GetValue(Item);
                            Obj(Command[1]) = Value;
                            break;
                        }

                    default:
                        {
                            PropertyInfo pInfo;

                            try
                            {
                                pInfo = Item.GetType().GetProperties().First(x => x.Name.ToUpper() == PropertyName.ToUpper());
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("SetPropertyValue unable to find property: " + PropertyName + " in objecttype: " + Value.GetType().ToString(), ex);
                            }

                            pInfo.SetValue(Item, CTypeDynamic(Value, pInfo.PropertyType));
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to set Property: " + PropertyName + " On Type: " + Item.GetType().ToString() + " With Value: " + Value?.ToString, ex);
            }
        }
        private static void SetPropertyValuePath(object Item, string Path, object Value)
        {
            List<string> lNav = GetObjectNavigation(Path);

            object Obj = Item;

            for (int i = 1; i <= lNav.Count - 1; i++)
            {
                if (i == lNav.Count - 1)
                {
                    Item.SetPropertyValue(lNav[i], Value);
                    return;
                }
                else
                    Obj = Item.GetPropertyValue(lNav[i]);
            }


            throw new Exception("Unable to find property: " + Path);
        }

        private static List<string> GetObjectNavigation(string Nav)
        {
            return Regex.Matches(Nav, "{(.*?)}").Cast<Match>().Select(x => x.Groups(1).Value).ToList();
        }

        public static object GetPropertyValue<T>(this T Item, string PropertyName, Dictionary<int, Dictionary<string, string>> Translation)
        {
            try
            {
                switch (PropertyName.ToCharArray()[0])
                {
                    case '#' // Dictionary/Hashset, any object that has a string indexed name value pair
                   :
                        {
                            string[] Command = PropertyName.Split(',');
                            Command[0] = Command[0].Substring(1);
                            dynamic Obj = Item.GetType().GetProperties().First(x => x.Name.ToString().ToUpper() == Command[0].GetValue(Item));
                            return Obj(Command[1]); // This returns the inner object's value for the specified property
                        }

                    case '$':
                        {
                            string Command = PropertyName.Substring(1);
                            return Command;
                        }

                    case '^':
                        {
                            if (Translation != null)
                            {
                                if (Translation.Count > 0)
                                {
                                    // LookupTT:CRD

                                    string[] Command = PropertyName.Split(',');
                                    Command[0] = Command[0].Substring(1);

                                    List<string> ItemProperties = Item.GetType().GetProperties().Select(x => x.Name.ToUpper()).ToList();

                                    if (Item.GetType().GetProperties().Select(x => x.Name.ToUpper()).ToList().Contains(Command[1].ToUpper()) == false)
                                        return null;

                                    string OrigValue = Item.GetType().GetProperties().First(x => x.Name.ToUpper() == Command[1].ToUpper()).GetValue(Item).ToString();
                                    string Value = null;

                                    if (Translation.Keys.Contains(Command[0]))
                                    {
                                        if (Translation(Command[0]).Item(0) == "{*}")
                                            Value = Translation(Command[0]).Values(0).Replace("{*}", OrigValue);
                                        else
                                            Value = Translation(Command[0])(OrigValue.ToUpper());
                                    }

                                    // If Translation was not found, returning the current value.
                                    if (Value == null)
                                        return OrigValue;

                                    return Value;
                                }
                                else
                                    // No Translation Sent in, Pass the current value
                                    return Item.GetType().GetProperties().First(x => x.Name.ToUpper() == PropertyName.ToUpper()).GetValue(Item);
                            }
                            else
                                // No Translation Sent in, Pass the current value
                                return Item.GetType().GetProperties().First(x => x.Name.ToUpper() == PropertyName.ToUpper()).GetValue(Item);
                            break;
                        }

                    default:
                        {
                            return Item.GetType().GetProperties().First(x => x.Name.ToUpper() == PropertyName.ToUpper()).GetValue(Item);
                        }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Getting Property: " + PropertyName + " On Type: " + Item.GetType().ToString(), ex);
            }
        }

    }
}
