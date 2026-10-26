(function () {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const sinFoto = 'data:image/svg+xml;utf8,' + encodeURIComponent(
        '<svg xmlns="http://www.w3.org/2000/svg" width="96" height="72"><rect width="100%" height="100%" fill="#e9ecef"/>' +
        '<text x="50%" y="55%" text-anchor="middle" font-size="11" fill="#6c757d" font-family="sans-serif">Sin foto</text></svg>');
    const formatoMoneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });

    async function api(url, opciones = {}) {
        const headers = Object.assign({ RequestVerificationToken: token }, opciones.headers || {});
        const resp = await fetch(url, Object.assign({}, opciones, { headers, credentials: 'same-origin' }));
        let datos = null;
        try { datos = await resp.json(); } catch (e) { /* respuesta sin cuerpo */ }
        if (!resp.ok) {
            const err = new Error('http ' + resp.status);
            err.errores = (datos && datos.errores) || ['Ocurrió un error inesperado (' + resp.status + '). Probá de nuevo.'];
            throw err;
        }
        return datos;
    }

    const post = (url) => api(url, { method: 'POST' });
    const postJson = (url, cuerpo) => api(url, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo)
    });

    Vue.createApp({
        data() {
            return {
                items: [], total: 0, pagina: 1, tamanio: 10, cargando: false,
                destinos: [], tipos: [], sinFoto,
                filtro: { buscar: '', destinoId: '', tipoId: '', estado: '' },
                mensaje: '', errorListado: '', temporizador: null, temporizadorMsg: null,
                form: { abierto: false, guardando: false, errores: [], datos: {} },
                panelFotos: { abierto: false, hospedaje: {}, fotos: [], errores: [], subiendo: false }
            };
        },
        computed: {
            totalPaginas() { return Math.max(1, Math.ceil(this.total / this.tamanio)); },
            destinosParaForm() {
                // Solo destinos activos, más el que ya tiene el hospedaje que se edita.
                return this.destinos.filter(d => d.activo || d.id === this.form.datos.destinoId);
            }
        },
        watch: {
            filtro: {
                deep: true,
                handler() {
                    clearTimeout(this.temporizador);
                    this.temporizador = setTimeout(() => { this.pagina = 1; this.cargar(); }, 300);
                }
            }
        },
        async mounted() {
            window.addEventListener('keydown', (e) => {
                if (e.key !== 'Escape') return;
                if (this.panelFotos.abierto) this.cerrarFotos();
                else this.form.abierto = false;
            });
            try {
                const o = await api('/Hospedajes/Opciones');
                this.destinos = o.destinos;
                this.tipos = o.tipos;
            } catch (e) { this.errorListado = e.errores.join(' '); }
            this.cargar();
        },
        methods: {
            moneda(n) { return formatoMoneda.format(n); },
            avisar(texto) {
                this.mensaje = texto;
                clearTimeout(this.temporizadorMsg);
                this.temporizadorMsg = setTimeout(() => { this.mensaje = ''; }, 4000);
            },
            async cargar() {
                this.cargando = true;
                this.errorListado = '';
                const p = new URLSearchParams({ pagina: this.pagina, tamanio: this.tamanio });
                if (this.filtro.buscar.trim()) p.set('buscar', this.filtro.buscar.trim());
                if (this.filtro.destinoId) p.set('destinoId', this.filtro.destinoId);
                if (this.filtro.tipoId) p.set('tipoId', this.filtro.tipoId);
                if (this.filtro.estado) p.set('estado', this.filtro.estado);
                try {
                    const r = await api('/Hospedajes/Listar?' + p.toString());
                    this.items = r.items;
                    this.total = r.total;
                    if (this.pagina > this.totalPaginas) { this.pagina = this.totalPaginas; return this.cargar(); }
                } catch (e) {
                    this.errorListado = e.errores.join(' ');
                } finally {
                    this.cargando = false;
                }
            },
            irA(n) { this.pagina = n; this.cargar(); },

            nuevo() {
                this.form = {
                    abierto: true, guardando: false, errores: [],
                    datos: { id: 0, nombre: '', destinoId: '', tipoId: '', direccion: '', capacidadMaxima: 1, precioNoche: 0, servicios: '' }
                };
            },
            editar(h) {
                this.form = {
                    abierto: true, guardando: false, errores: [],
                    datos: {
                        id: h.id, nombre: h.nombre, destinoId: h.destinoId, tipoId: h.tipoId,
                        direccion: h.direccion || '', capacidadMaxima: h.capacidadMaxima,
                        precioNoche: h.precioNoche, servicios: h.servicios || ''
                    }
                };
            },
            async guardar() {
                const d = this.form.datos;
                const errores = [];
                if (!d.nombre || !d.nombre.trim()) errores.push('El nombre es obligatorio.');
                if (!d.tipoId) errores.push('Elegí un tipo de hospedaje.');
                if (!d.destinoId) errores.push('Elegí un destino.');
                if (!(d.capacidadMaxima > 0)) errores.push('La capacidad máxima debe ser mayor a cero.');
                if (d.precioNoche === '' || d.precioNoche < 0) errores.push('El precio por noche no puede ser negativo.');
                if (errores.length) { this.form.errores = errores; return; }

                this.form.guardando = true;
                this.form.errores = [];
                try {
                    const nuevo = !d.id;
                    await postJson('/Hospedajes/Guardar', Object.assign({}, d, { activo: true }));
                    this.form.abierto = false;
                    this.avisar(nuevo ? 'Hospedaje creado.' : 'Cambios guardados.');
                    this.cargar();
                } catch (e) {
                    this.form.errores = e.errores;
                } finally {
                    this.form.guardando = false;
                }
            },
            async cambiarActivo(h) {
                const accion = h.activo ? 'desactivar' : 'activar';
                if (!confirm('¿Querés ' + accion + ' "' + h.nombre + '"?')) return;
                try {
                    await post('/Hospedajes/CambiarActivo?id=' + h.id + '&activo=' + (!h.activo));
                    this.avisar(h.activo ? 'Hospedaje desactivado.' : 'Hospedaje activado.');
                    this.cargar();
                } catch (e) { this.errorListado = e.errores.join(' '); }
            },
            async eliminar(h) {
                if (!confirm('¿Eliminar "' + h.nombre + '" y sus fotos? Esta acción no se puede deshacer.')) return;
                try {
                    await post('/Hospedajes/Eliminar?id=' + h.id);
                    this.avisar('Hospedaje eliminado.');
                    this.cargar();
                } catch (e) { this.errorListado = e.errores.join(' '); }
            },

            async abrirFotos(h) {
                this.panelFotos = { abierto: true, hospedaje: h, fotos: [], errores: [], subiendo: false };
                await this.cargarFotos();
            },
            cerrarFotos() { this.panelFotos.abierto = false; this.cargar(); },
            async cargarFotos() {
                try { this.panelFotos.fotos = await api('/Hospedajes/Fotos/' + this.panelFotos.hospedaje.id); }
                catch (e) { this.panelFotos.errores = e.errores; }
            },
            async subirFotos(ev) {
                const input = ev.target;
                if (!input.files.length) return;
                const datos = new FormData();
                for (const f of input.files) datos.append('archivos', f);
                this.panelFotos.subiendo = true;
                this.panelFotos.errores = [];
                try {
                    const r = await api('/Hospedajes/SubirFoto/' + this.panelFotos.hospedaje.id, { method: 'POST', body: datos });
                    this.panelFotos.errores = r.errores;
                    await this.cargarFotos();
                } catch (e) {
                    this.panelFotos.errores = e.errores;
                } finally {
                    this.panelFotos.subiendo = false;
                    input.value = '';
                }
            },
            async hacerPortada(f) {
                try { await post('/Hospedajes/MarcarPortada/' + f.id); this.panelFotos.errores = []; await this.cargarFotos(); }
                catch (e) { this.panelFotos.errores = e.errores; }
            },
            async borrarFoto(f) {
                if (!confirm('¿Eliminar esta foto?')) return;
                try { await post('/Hospedajes/EliminarFoto/' + f.id); this.panelFotos.errores = []; await this.cargarFotos(); }
                catch (e) { this.panelFotos.errores = e.errores; }
            }
        }
    }).mount('#app');
})();
