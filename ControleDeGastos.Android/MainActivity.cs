﻿using Android.App;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System;
using System.Linq;
using Android.Content;
using Android.Runtime;

namespace ControleDeGastos.Android
{
    [Activity(Label = "Controle de Gastos", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private ExpandableListView _listViewGastos;
        private Button _buttonNovo;
        private Button _buttonLimpar;

        public static List<Models.Estabelecimento> Estabelecimentos { get; private set; }
        private List<Models.Gasto> _gastos;
        private List<ListViewGroup> _listViewGroups;
        private ListViewAdapter _adapter;

        protected override void OnCreate(Bundle bundle)
        {
            var culture = new System.Globalization.CultureInfo("de-DE");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            base.OnCreate(bundle);

            _gastos = CarregarGastos();

            SetContentView(Resource.Layout.Main);

            _listViewGastos = FindViewById<ExpandableListView>(Resource.Id.listViewGastos);
            _listViewGroups = PrepararListViewGroups(_gastos);
            _adapter = new ListViewAdapter(this, _listViewGroups);
            _listViewGastos.SetAdapter(_adapter);
            _listViewGastos.ChildClick += ListViewGastos_ChildClick;
            ExpandirTodosOsGruposDoListView();

            _buttonNovo = FindViewById<Button>(Resource.Id.buttonNovo);
            _buttonNovo.Click += buttonNovo_Click;

            _buttonLimpar = FindViewById<Button>(Resource.Id.buttonLimpar);
            _buttonLimpar.Click += buttonLimpar_Click;
        }

        private void buttonLimpar_Click(object sender, EventArgs e)
        {
            _gastos.Clear();
            _listViewGroups.Clear();
            _adapter.NotifyDataSetChanged();
        }

        private void buttonNovo_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(EditarGastoActivity));
            intent.PutExtra("Id", -1);
            intent.PutExtra("Data", DateTime.Now.Date.Ticks);
            StartActivityForResult(intent, 0);
        }

        private void ExpandirTodosOsGruposDoListView()
        {
            for (int group = 0; group <= _adapter.GroupCount - 1; group++)
            {
                _listViewGastos.ExpandGroup(group);
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == Result.Ok)
            {
                var id = data.Extras.GetInt("Id");
                var gasto = _gastos.FirstOrDefault(g => g.Id == id);

                if (gasto == null)
                {
                    gasto = new Models.Gasto()
                    {
                        Id = 1
                    };
                    if (_gastos.Any())
                    {
                        gasto.Id = _gastos.Max(g => g.Id) + 1;
                    }
                    _gastos.Add(gasto);
                }

                var dataAnterior = gasto.Data.Date;
                var dataNova = new DateTime(data.Extras.GetLong("Data")).Date;
                gasto.Data = dataNova;
                gasto.Valor = Convert.ToDecimal(data.Extras.GetDouble("Valor"));
                var nomeEstabelecimento = data.Extras.GetString("Estabelecimento");
                var estabelecimento = Estabelecimentos.FirstOrDefault(e => string.Compare(e.Nome, nomeEstabelecimento, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (estabelecimento == null)
                {
                    estabelecimento = new Models.Estabelecimento()
                    {
                        Nome = nomeEstabelecimento
                    };
                }
                gasto.Estabelecimento = estabelecimento;

                if (!dataAnterior.Equals(dataNova))
                {
                    var listViewGroupAnterior = _listViewGroups.FirstOrDefault(lvg => lvg.Data.Equals(dataAnterior));
                    if (listViewGroupAnterior != null)
                    {
                        listViewGroupAnterior.Gastos.Remove(gasto);
                    }

                    var listViewGroupNovo = _listViewGroups.FirstOrDefault(lvg => lvg.Data.Equals(dataNova));
                    if (listViewGroupNovo == null)
                    {
                        listViewGroupNovo = new ListViewGroup()
                        {
                            Data = dataNova
                        };
                        _listViewGroups.Add(listViewGroupNovo);
                    }
                    listViewGroupNovo.Gastos.Add(gasto);
                    _listViewGroups.Sort((lvg1, lvg2) => lvg1.Data.CompareTo(lvg2.Data));
                }

                _adapter.NotifyDataSetChanged();
                ExpandirTodosOsGruposDoListView();
            }
        }

        private void ListViewGastos_ChildClick(object sender, ExpandableListView.ChildClickEventArgs e)
        {
            var infoGrupo = _listViewGroups[e.GroupPosition];
            var gasto = infoGrupo.Gastos[e.ChildPosition];
            var intent = new Intent(this, typeof(EditarGastoActivity));
            intent.PutExtra("Id", gasto.Id);
            intent.PutExtra("Data", infoGrupo.Data.Ticks);
            intent.PutExtra("Estabelecimento", gasto.Estabelecimento.Nome);
            intent.PutExtra("Valor", Convert.ToDouble(gasto.Valor));
            StartActivityForResult(intent, 0);
        }

        private List<Models.Gasto> CarregarGastos()
        {
            Estabelecimentos = new List<Models.Estabelecimento>();
            for (int c = 1; c <= 10; c++)
            {
                Estabelecimentos.Add(new Models.Estabelecimento() { Id = c, Nome = string.Format("Estabelecimento {0}", c) });
            }

            var random = new Random();
            var gastos = new List<Models.Gasto>();
            for (int c = 1; c <= 15; c++)
            {
                var data = DateTime.Now.AddDays(random.Next(0, 3));
                var estabelecimento = Estabelecimentos[random.Next(0, Estabelecimentos.Count - 1)];
                var valor = random.Next(1, 50);
                gastos.Add(new Models.Gasto() { Id = c, Data = data, Estabelecimento = estabelecimento, Valor = valor });
            }

            return gastos;
        }

        private List<ListViewGroup> PrepararListViewGroups(List<Models.Gasto> gastos)
        {
            var listViewGroups = new List<ListViewGroup>();

            if (gastos.Any())
            {
                var gastosAgrupados = from gasto in gastos
                                      orderby gasto.Data
                                      group gasto by gasto.Data.Date into grupoDeGastos
                                      select new
                                      {
                                          Data = grupoDeGastos.Key,
                                          Gastos = grupoDeGastos
                                      };

                foreach (var gastoAgrupado in gastosAgrupados)
                {
                    var listViewGroup = new ListViewGroup() { Data = gastoAgrupado.Data };
                    listViewGroups.Add(listViewGroup);
                    listViewGroup.Gastos.AddRange(gastoAgrupado.Gastos);
                }
            }

            return listViewGroups;
        }
    }
}

