import { Component, OnDestroy, OnInit, AfterViewInit, ViewChild, ElementRef, signal, effect, computed, viewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DnsStats } from './stats.interface';
import { Chart } from 'chart.js/auto';
import { HourlyStat } from './hourlystat.interface';
import { ClientStat } from './clientstat.interface';
import { DnsLogEntry } from './dnslogentry.interface';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy, AfterViewInit {
  stats = signal<DnsStats>({ totalQueries: 0, blockedQueries: 0, recentBlocks: [] });
  history = signal<HourlyStat[]>([]);
  clients = signal<ClientStat[]>([]);
  logs = signal<DnsLogEntry[]>([]);
  searchTerm = signal<string>('');
  filterType = signal<string>('ALL');
  filteredLogs = computed(() => {
    let currentLogs = this.logs();
    const term = this.searchTerm().toLowerCase();
    const type = this.filterType();

    if (type === 'BLOCKED') currentLogs = currentLogs.filter(l => l.isBlocked);
    if (type === 'FORWARDED') currentLogs = currentLogs.filter(l => !l.isBlocked);

    if (term) {
      currentLogs = currentLogs.filter(l => 
        l.domain.toLowerCase().includes(term) || 
        l.clientIp.includes(term)
      );
    }
    return currentLogs;
  });

  private intervalId: any;
  dnsChart = viewChild.required<ElementRef<HTMLCanvasElement>>('dnsChart');
  historyChart = viewChild.required<ElementRef<HTMLCanvasElement>>('historyChart');
  clientsChart = viewChild.required<ElementRef<HTMLCanvasElement>>('clientsChart');
  donutChartInst: any;
  lineChartInst: any;
  barChartInst: any;

  constructor(private http: HttpClient) {
    effect(() => {
      const s = this.stats();
      if (this.donutChartInst) {
        const clean = Math.max(0, s.totalQueries - s.blockedQueries);
        this.donutChartInst.data.datasets[0].data = [clean, s.blockedQueries];
        this.donutChartInst.update('none');
      }
    });

    effect(() => {
      const histData = this.history();
      if (this.lineChartInst && histData.length > 0) {
        const labels = histData.map(d => new Date(d.hour).getHours() + ':00');
        const total = histData.map(d => d.totalQueries);
        const blocked = histData.map(d => d.blockedQueries);
        this.lineChartInst.data.labels = labels;
        this.lineChartInst.data.datasets[0].data = total;
        this.lineChartInst.data.datasets[1].data = blocked;
        this.lineChartInst.update('none');
      }
    });

    effect(() => {
      const clientData = this.clients();
      if (this.barChartInst && clientData.length > 0) {
        this.barChartInst.data.labels = clientData.map(c => c.ip);
        this.barChartInst.data.datasets[0].data = clientData.map(c => c.count);
        this.barChartInst.update('none');
      }
    });
  }

  ngOnInit() {
    this.fetchData();
    this.intervalId = setInterval(() => this.fetchData(), 2000);
  }

  fetchData() {
    this.http.get<DnsStats>('http://localhost:5000/api/stats').subscribe({
      next: (data) => this.stats.set(data),
      error: (err) => console.error('API Error (Stats):', err)
    });

    this.http.get<HourlyStat[]>('http://localhost:5000/api/stats/history').subscribe({
      next: (data) => this.history.set(data),
      error: (err) => console.error('API Error (History):', err)
    });

    this.http.get<ClientStat[]>('http://localhost:5000/api/stats/clients').subscribe({
      next: (data) => this.clients.set(data),
      error: (err) => console.error('API Error (Clients):', err)
    });

    // Φέρνουμε τα δεδομένα για τον Live Πίνακα
    this.http.get<DnsLogEntry[]>('http://localhost:5000/api/logs').subscribe({
      next: (data) => this.logs.set(data),
      error: (err) => console.error('API Error (Logs):', err)
    });
  }

  updateSearch(event: Event) {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  updateFilter(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.filterType.set(select.value);
  }

  ngAfterViewInit() {
    this.donutChartInst = new Chart(this.dnsChart().nativeElement, {
      type: 'doughnut',
      data: {
        labels: ['Καθαρά', 'Μπλοκαρισμένα'],
        datasets: [{ data: [1, 0], backgroundColor: ['#00d2ff', '#ff4757'], borderWidth: 0 }]
      },
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { labels: { color: 'white' } } } }
    });

    this.lineChartInst = new Chart(this.historyChart().nativeElement, {
      type: 'line',
      data: {
        labels: [],
        datasets: [
          { label: 'Συνολικά Αιτήματα', data: [], borderColor: '#00d2ff', backgroundColor: 'rgba(0, 210, 255, 0.1)', borderWidth: 2, fill: true, tension: 0.4 },
          { label: 'Μπλοκαρισμένα', data: [], borderColor: '#ff4757', backgroundColor: 'rgba(255, 71, 87, 0.1)', borderWidth: 2, fill: true, tension: 0.4 }
        ]
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        scales: { y: { beginAtZero: true, grid: { color: '#333' }, ticks: { color: '#aaa' } }, x: { grid: { color: '#333' }, ticks: { color: '#aaa' } } },
        plugins: { legend: { labels: { color: 'white' } } }
      }
    });

    this.barChartInst = new Chart(this.clientsChart().nativeElement, {
      type: 'bar',
      data: {
        labels: [],
        datasets: [{ label: 'Μπλοκαρισμένα Αιτήματα', data: [], backgroundColor: '#ff4757', borderRadius: 4 }]
      },
      options: {
        indexAxis: 'y', responsive: true, maintainAspectRatio: false,
        scales: { x: { beginAtZero: true, grid: { color: '#333' }, ticks: { color: '#aaa' } }, y: { grid: { display: false }, ticks: { color: '#aaa' } } },
        plugins: { legend: { display: false } }
      }
    });
  }

  ngOnDestroy() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }
}
