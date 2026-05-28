using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicaVeterinaria.Models;
using ClinicaVeterinaria.Services;
using ClinicaVeterinaria.Utils;

namespace ClinicaVeterinaria.Forms;

public sealed class FormDashboard : Form
{
    private readonly DashboardService _servicio = new();
    private readonly Dictionary<string, Label> _valores = new(StringComparer.OrdinalIgnoreCase);
    private DataGridView _gridAgenda = null!;
    private DateTimePicker _fecha = null!;
    private Label _lblRolVista = null!;

    public FormDashboard()
    {
        ConstruirInterfaz();
        ConfigurarEventos();
        CargarDatos();
    }

    private void ConstruirInterfaz()
    {
        UiTheme.PrepararFormulario(this);
        BackColor = UiTheme.Fondo;
        Padding = new Padding(10);

        Panel cabecera = new()
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = UiTheme.Fondo
        };
        Controls.Add(cabecera);

        Label titulo = UiTheme.CrearTitulo("Dashboard operativo");
        titulo.Location = new Point(4, 8);
        cabecera.Controls.Add(titulo);

        _lblRolVista = new Label
        {
            AutoSize = true,
            ForeColor = UiTheme.TextoSecundario,
            Location = new Point(7, 45)
        };
        cabecera.Controls.Add(_lblRolVista);

        Button actualizar = UiTheme.CrearBoton("Actualizar", true);
        actualizar.Width = 112;
        actualizar.Location = new Point(Width - 135, 16);
        actualizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        actualizar.Click += (_, _) => CargarDatos();
        cabecera.Controls.Add(actualizar);

        _fecha = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 120,
            Location = new Point(Width - 275, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        cabecera.Controls.Add(_fecha);

        FlowLayoutPanel tarjetas = new()
        {
            Dock = DockStyle.Top,
            Height = 206,
            AutoScroll = true,
            WrapContents = true,
            BackColor = UiTheme.Fondo,
            Padding = new Padding(0, 4, 0, 4)
        };
        Controls.Add(tarjetas);
        tarjetas.BringToFront();
        cabecera.BringToFront();

        AgregarTarjeta(tarjetas, "Citas de hoy", "CitasHoy");
        AgregarTarjeta(tarjetas, "En espera", "PacientesEnEspera");
        AgregarTarjeta(tarjetas, "En consulta", "PacientesEnConsulta");
        AgregarTarjeta(tarjetas, "Terminadas hoy", "ConsultasTerminadasHoy");
        AgregarTarjeta(tarjetas, "Canceladas / no asistió", "CanceladasNoAsistidasHoy");
        AgregarTarjeta(tarjetas, "Facturas pendientes", "FacturasPendientes");
        AgregarTarjeta(tarjetas, "Cobrado hoy", "TotalCobradoHoy");
        AgregarTarjeta(tarjetas, "Recordatorios próximos", "RecordatoriosProximos");
        AgregarTarjeta(tarjetas, "Stock bajo", "ProductosStockBajo");
        AgregarTarjeta(tarjetas, "Por vencer", "ProductosPorVencer");

        Panel panelAgenda = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18)
        };
        Controls.Add(panelAgenda);
        panelAgenda.BringToFront();

        Label agendaTitulo = new()
        {
            Text = "Agenda del día",
            Dock = DockStyle.Top,
            Height = 38,
            Font = UiTheme.FuenteSubtitulo,
            ForeColor = UiTheme.Texto
        };
        panelAgenda.Controls.Add(agendaTitulo);

        _gridAgenda = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.PrepararGrid(_gridAgenda);
        panelAgenda.Controls.Add(_gridAgenda);
        _gridAgenda.BringToFront();
    }

    private void ConfigurarEventos()
    {
        _fecha.ValueChanged += (_, _) => CargarDatos();
        _gridAgenda.CellContentClick += GridAgenda_CellContentClick;
    }

    private void AgregarTarjeta(FlowLayoutPanel contenedor, string titulo, string clave)
    {
        Panel tarjeta = new()
        {
            Width = 207,
            Height = 88,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 12, 10),
            Padding = new Padding(13, 9, 13, 8)
        };

        Label lblTitulo = new()
        {
            Text = titulo,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = UiTheme.TextoSecundario
        };
        Label valor = new()
        {
            Text = "0",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = UiTheme.Primario,
            TextAlign = ContentAlignment.MiddleLeft
        };
        tarjeta.Controls.Add(valor);
        tarjeta.Controls.Add(lblTitulo);
        _valores[clave] = valor;
        contenedor.Controls.Add(tarjeta);
    }

    private void CargarDatos()
    {
        try
        {
            UsuarioModel? usuario = SesionActual.Usuario;
            _lblRolVista.Text = usuario?.Rol == "Veterinario"
                ? "Vista limitada a las citas asignadas al veterinario autenticado."
                : $"Vista operativa para el rol: {usuario?.Rol ?? "Sin sesión"}.";

            DashboardResumenModel resumen = _servicio.ObtenerResumen(_fecha.Value.Date);
            _valores["CitasHoy"].Text = resumen.CitasHoy.ToString("N0");
            _valores["PacientesEnEspera"].Text = resumen.PacientesEnEspera.ToString("N0");
            _valores["PacientesEnConsulta"].Text = resumen.PacientesEnConsulta.ToString("N0");
            _valores["ConsultasTerminadasHoy"].Text = resumen.ConsultasTerminadasHoy.ToString("N0");
            _valores["CanceladasNoAsistidasHoy"].Text = resumen.CanceladasNoAsistidasHoy.ToString("N0");
            _valores["FacturasPendientes"].Text = resumen.FacturasPendientes.ToString("N0");
            _valores["TotalCobradoHoy"].Text = resumen.TotalCobradoHoy.ToString("C2");
            _valores["RecordatoriosProximos"].Text = resumen.RecordatoriosProximos.ToString("N0");
            _valores["ProductosStockBajo"].Text = resumen.ProductosStockBajo.ToString("N0");
            _valores["ProductosPorVencer"].Text = resumen.ProductosPorVencer.ToString("N0");

            List<AgendaDashboardItemModel> agenda = _servicio.ObtenerAgendaDelDia(_fecha.Value.Date);
            ConfigurarColumnasAgenda();
            _gridAgenda.DataSource = agenda;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo cargar el dashboard.\n\n{ex.Message}",
                "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ConfigurarColumnasAgenda()
    {
        _gridAgenda.AutoGenerateColumns = false;
        _gridAgenda.Columns.Clear();
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Hora",
            HeaderText = "Hora",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "HH:mm" },
            FillWeight = 55
        });
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Mascota", HeaderText = "Mascota" });
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dueno", HeaderText = "Dueño", FillWeight = 130 });
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Veterinario", HeaderText = "Veterinario", FillWeight = 120 });
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Servicio", HeaderText = "Servicio" });
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 80 });
        _gridAgenda.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "SaldoPendiente",
            HeaderText = "Saldo",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
            FillWeight = 70
        });
        _gridAgenda.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "Acción",
            Text = "Ver",
            UseColumnTextForButtonValue = true,
            FillWeight = 55
        });
    }

    private void GridAgenda_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != _gridAgenda.Columns.Count - 1)
        {
            return;
        }

        if (_gridAgenda.Rows[e.RowIndex].DataBoundItem is AgendaDashboardItemModel cita)
        {
            MessageBox.Show(
                $"Cita #{cita.IdCita}\n{cita.Hora:dd/MM/yyyy HH:mm} - {cita.Mascota}\nEstado: {cita.Estado}\n\n" +
                "Las acciones clínicas y de agenda se activan en los macrobloques correspondientes.",
                "Detalle de cita", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
