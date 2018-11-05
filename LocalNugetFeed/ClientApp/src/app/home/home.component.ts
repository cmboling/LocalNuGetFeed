import {Component, ElementRef, ViewChild} from "@angular/core";
import {Package} from "../shared/models/package.model";
import {PackageService} from "../services/package.service";
import {ActivatedRoute, Router} from "@angular/router";

@Component({
  selector: 'home',
  templateUrl: './home.html',
  styleUrls: ['./home.component.css']
})

export class HomeComponent {
  public packages: Package[] = [];
  public loading = false;
  query = '';

  constructor(private _packageService: PackageService, private router: Router, private activeRoute: ActivatedRoute) {
  }

  @ViewChild('searchRef', { read: ElementRef }) searchRef: ElementRef | any;

  ngOnInit() {
    const queryParams = this.activeRoute.snapshot.queryParams;

    if (queryParams['q']) {
      this.query = queryParams['q'];
    }
    this.search(this.query);
  }


  search(query?: string) {
    this.loading = true;

    this._packageService.search(query).subscribe((result: Package[]) => {
        this.packages = result || [];
      }, (error) => {
        this.packages.length = 0;
        console.log(error);
      }, () => {
        this.loading = false;
      }
    );
  }
}
