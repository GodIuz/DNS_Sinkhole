import { Component, OnDestroy, OnInit, AfterViewInit, ViewChild, ElementRef, signal, effect, viewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DnsStats } from './stats.interface';
import { Chart } from 'chart.js/auto';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy, AfterViewInit {
  stats = signal<DnsStats>({ totalQueries: 0, blockedQueries: 0, recentBlocks: [] });
  private intervalId: any;
 dnsChart = viewChild.required<ElementRef<HTMLCanvasElement>>('dnsChart');
  chart: any;

  constructor(private http: HttpClient) {
    effect(() => {
      const s = this.stats();
      if (this.chart) {
        this.updateChart();
      }
    });
  }

  ngOnInit() {
    this.fetchStats();
    this.intervalId = setInterval(() => this.fetchStats(), 2000);
  }

  fetchStats() {
    this.http.get<DnsStats>('http://localhost:5000/api/stats').subscribe({
      next: (data) => this.stats.set(data),
      error: (err) => console.error('Το API δεν απαντάει:', err)
    });
  }

 ngAfterViewInit() {
    this.chart = new Chart(this.dnsChart().nativeElement, {
      type: 'doughnut',
      data: {
        labels: ['Καθαρά', 'Μπλοκαρισμένα'],
        datasets: [{ 
            data: [1, 0],
            backgroundColor: ['#00d2ff', '#ff4757']
        }]
      },
      options: { responsive: true, maintainAspectRatio: false }
    });
  }

  updateChart() {
    const total = this.stats().totalQueries;
    const blocked = this.stats().blockedQueries;
    const clean = Math.max(0, total - blocked);
    
    this.chart.data.datasets[0].data = [clean, blocked];
    this.chart.update('none');
  }

  ngOnDestroy() {
    clearInterval(this.intervalId);
  }
}