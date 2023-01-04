

# EntityFramework Oracle Core Procedure Generator , a Stored Procedure and UDDT Scaffolder for .Netcore 

This command line utility has been created to simplify the stored procedures and UDDTs management in .Netcore.

With this tool you can rip Oracle database and get a new partial context files with all the stored procedures methods and another with all UDDTs.

It was inspired by DarioN1's [SPTOCore](https://github.com/DarioN1/SPToCore)

## Installation

Download "output\EF.Oracle.Core.Procedure.Generator.exe" file.

## Usage

```
EF.Oracle.Core.Procedure.Generator --connection "Data Source=(DESCRIPTION=(CONNECT_DATA=(SERVICE_NAME=service-name))(ADDRESS=(PROTOCOL=TCP)(HOST=host)(PORT=port)));User ID=user-id;Password=password;persist security info=false;Connection Timeout=120;" 
 --schema Workers 
 --context WorkersModelContext 
 --sfolder Model 
 --pfolder C:\\MyProject\\ef-oracle-core-procedure-generator\\EF.Oracle.Core.Procedure.Generator\\ 
 --fileName WorkersModelContext.cs 
 --namespace Workers.Database.Models"
```


## Implementation

Unfortunately I didn’t found a way to predict the store-procedure return result (like in SQL server)
I could only predict if it has one.

Those without a return value return  ``Task<int>`` which indicates number of rows which were affected.
Therefore the Pocos should be created manually according to procedure main select closure.
I left the procedure return value as a template.

### Example 1 - Simple "Select" Store Procedure

#### The  Procedure sql  code
```sql
CREATE OR REPLACE PROCEDURE USP__PRODUCT_GET
(
     v_DATE            IN      DATE
)
AS
v_cursor SYS_REFCURSOR;
  BEGIN
        OPEN  v_cursor FOR
         SELECT
          prd.PRODUCT_ID ,
          prd.PRODUCT_NAME ,
          prd.PRODUCT_VALUE
        FROM  Products prd 
	WHERE  prd.EXPIRY_DATE = v_DATE;

   DBMS_SQL.RETURN_RESULT(v_cursor);

  END USP__PRODUCT_GET;
```  

#### The generated Procedure code
```c#
public async Task<List<T>> USP__PRODUCT_GET_Async<T>(DateTime? V_DATE) where T : class
{
            // Parameters
            OracleParameter p_V_DATE = new OracleParameter();
            p_V_DATE .Direction = ParameterDirection.Input;
            p_V_DATE .OracleDbType = OracleDbType.Date;
            p_V_DATE .Value = V_DATE?? (object)DBNull.Value;
            p_V_DATE .ParameterName = "V_DATE";
            // Processing 
            string sqlQuery = $@"BEGIN USP__MD_PRODUCT_GET(:V_DATE); END;";
            
            //Execution
            var res = this.Set<T>().FromSqlRaw(sqlQuery, p_V_DATE);
            return await res.ToListAsync();
}
```            
#### The usage

```c#
public async Task<List<ProductPoco>> GetProductsByDates(DateTime dateTime)
{
    try
    {
	       await using var context = new ModelContext();
           var retVal = await context.USP__MD_PRODUCT_GET_Async<ProductPoco>(dateTime);
           return retVal.ToList();
     }
     catch (Exception e)
     {
           _logger.LogError(e, "Problem in GetProductsByDates");
           throw;
     }
}
```       


#### The Poco (manually added)

```c#
public class ProductPoco
{
     public string PRODUCT_ID { get; set; }
     public string PRODUCT_NAME { get; set; }
     public double PRODUCT_VALUE { get; set; }
}
```     

### Example 2 - Store Procedure with UDDT

In this case the generated code is more complex.
In addition to generated Procedure you will get (in UDDT file)
Two more classes for each Type: 

 - The type definition with all the needed properties
 - A factory class which requiered to fill store-procedure parameter in runtime.



#### The  Procedure sql  code and UDDTs sql code
```sql

-- USP__PRODUCT_UPSERT
CREATE OR REPLACE PROCEDURE USP__PRODUCT_UPSERT
  (
    tt__PRODUCT       IN      TT__PRODUCT,
    v_EXPIRY_DATE            IN      DATE
  )
AS

  BEGIN

    MERGE INTO MD_PRODUCT DEST
    USING ( SELECT * FROM TABLE( tt__PRODUCT )) SRC
    ON (
       DEST.PRODUCT_ID = SRC.PRODUCT_ID 
    WHEN MATCHED THEN UPDATE SET  DEST.PRODUCT_NAME  = SRC.PRODUCT_NAME ,
                                  DEST.PRODUCT_VALUE  = SRC.PRODUCT_VALUE 
								  DEST.UPDATE_DATE = SYSTIMESTAMP,
								  DEST.EXPIRY_DATE = v_EXPIRY_DATE

    WHEN NOT MATCHED THEN INSERT (PRODUCT_NAME, PRODUCT_VALUE,EXPIRY_DATE, CREATED_DATE, UPDATE_DATE)
    VALUES( SRC.PRODUCT_ID, SRC.PRODUCT_NAME, SRC.PRODUCT_VALUE, v_EXPIRY_DATE, SYSTIMESTAMP,SYSTIMESTAMP);

  END USP__PRODUCT_UPSERT;


-- TT__MD_PRODUCT
CREATE OR REPLACE TYPE TT__MD_PRODUCT
            IS
            TABLE OF OBJ__PRODUCT;

-- OBJ__PRODUCT
CREATE OR REPLACE TYPE IMC."OBJ__PRODUCT" AS
            OBJECT
            (
            "PRODUCT_ID"   VARCHAR2(5 BYTE)  ,
            "PRODUCT_NAME" VARCHAR2(200 BYTE)  ,
            "PODUCT_VALUE" NUMBER(12,7) 
            );

```  

#### The generated Procedure code
```c#
 public async Task<int> USP__PRODUCT_UPSERT_Async(TT__PRODUCT TT__PRODUCT, DateTime? V_EXPIRY_DATE)
        {
            // Parameters

            OracleParameter p_TT__PRODUCT = new OracleParameter();
            p_TT__PRODUCT.Direction = ParameterDirection.Input;
            p_TT__PRODUCT.OracleDbType = OracleDbType.Array;
            p_TT__PRODUCT.Value = TT__PRODUCT ?? (object)DBNull.Value;
            p_TT__PRODUCT.ParameterName = "TT__PRODUCT";
            p_TT__PRODUCT.UdtTypeName = "TT__PRODUCT";

            OracleParameter p_V_EXPIRY_DATE = new OracleParameter();
            p_V_EXPIRY_DATE.Direction = ParameterDirection.Input;
            p_V_EXPIRY_DATE.OracleDbType = OracleDbType.Date;
            p_V_EXPIRY_DATE.Value = V_EXPIRY_DATE?? (object)DBNull.Value;
            p_V_EXPIRY_DATE.ParameterName = "V_EXPIRY_DATE";

            // Processing 
            string sqlQuery = $@"BEGIN USP__PRODUCT_UPSERT(:TT__PRODUCT, :V_EXPIRY_DATE); END;";

            return await this.Database.ExecuteSqlRawAsync(sqlQuery, p_TT__PRODUCT, p_V_EXPIRY_DATE );
}
```

#### The generated UDDTs code
```c#
	  #region TT__PRODUCT

        public class TT__PRODUCT : INullable, IOracleCustomType
        {
            private OBJ__PRODUCT[] _OBJ__PRODUCT;
            
            [OracleArrayMapping]
            public OBJ__MD_PRODUCT[] OBJ__PRODUCT
            {
                get => _OBJ__PRODUCT ;
                set
                {
                    try
                    {
                        _OBJ__PRODUCT = value;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error occurred setting value for OBJ__PRODUCT.", ex);
                    }
                }
            }


            public void FromCustomObject(OracleConnection con, object pUdt)
            {
                OracleUdt.SetValue(con, pUdt, 0, OBJ__PRODUCT);
            }

            public void ToCustomObject(OracleConnection con, object pUdt)
            {
                OBJ__PRODUCT = (OBJ__PRODUCT[])OracleUdt.GetValue(con, pUdt, "OBJ__PRODUCT");
            }

            public bool IsNull { get; private set; }

            public static TT__PRODUCT Null
            {
                get
                {
                    var obj = new TT__PRODUCT { IsNull = true };
                    return obj;
                }
            }
        }


        [OracleCustomTypeMapping("TT__PRODUCT")]
        public class TT__PRODUCT_Factory : IOracleCustomTypeFactory, IOracleArrayTypeFactory
        {
            public IOracleCustomType CreateObject()
            {
                return new TT__PRODUCT();
            }
            public Array CreateArray(int numElems)
            {
                return new OBJ__PRODUCT[numElems];
            }

            public Array CreateStatusArray(int numElems)
            {
                return null;
            }
        }

        #endregion

		#region OBJ__MD_PRODUCT

        public class OBJ__PRODUCT : INullable, IOracleCustomType
        {
            private String _PRODUCT_ID;

            /// <summary> Max Length: 5 </summary> 
            [OracleObjectMapping("PRODUCT_ID")]
            public String PRODUCT_ID
            {
                get => _PRODUCT_ID;
                set
                {
                    try
                    {

                        if (PRODUCT_ID != null && PRODUCT_ID.Length > 5)
                            throw new Exception($"String length too long. The maximum length valid for PRODUCT_ID is 5, but PRODUCT_ID length is [{PRODUCT_ID.Length}].");


                        _PRODUCT_ID = value;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error occurred setting value for PRODUCT_ID.", ex);
                    }
                }
            }
            private String PRODUCT_NAME;

            /// <summary> Max Length: 200 </summary> 
            [OracleObjectMapping("PRODUCT_NAME")]
            public String PRODUCT_NAME
            {
                get => PRODUCT_NAME;
                set
                {
                    try
                    {

                        if (PRODUCT_NAME != null && PRODUCT_NAME.Length > 200)
                            throw new Exception($"String length too long. The maximum length valid for PRODUCT_NAME is 200, but TERM_CURRENCY_PRODUCT_ID length is [{PRODUCT_NAME.Length}].");


                        PRODUCT_NAME = value;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error occurred setting value for PRODUCT_NAME.", ex);
                    }
                }
            }
            
            private Decimal? _PRODUCT_VALUE;
            
            [OracleObjectMapping("PRODUCT_VALUE")]
            public Decimal? PRODUCT_VALUE
            {
                get => _PRODUCT_VALUE;
                set
                {
                    try
                    {

                        _PRODUCT_VALUE = value;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error occurred setting value for PRODUCT_VALUE.", ex);
                    }
                }
            }
            
            public void FromCustomObject(OracleConnection con, object pUdt)
            {

                OracleUdt.SetValue(con, pUdt, "PRODUCT_ID", PRODUCT_ID);

                OracleUdt.SetValue(con, pUdt, "PRODUCT_NAME", PRODUCT_NAME);

                OracleUdt.SetValue(con, pUdt, "PRODUCT_VALUE", PRODUCT_VALUE);
            }

            public void ToCustomObject(OracleConnection con, object pUdt)
            {

                PRODUCT_ID = (String)OracleUdt.GetValue(con, pUdt, "PRODUCT_ID");
                PRODUCT_NAME = (String)OracleUdt.GetValue(con, pUdt, "PRODUCT_NAME");
                PRODUCT_VALUE = (Decimal?)OracleUdt.GetValue(con, pUdt, "PRODUCT_VALUE");
            }

            public bool IsNull { get; private set; }

            public static OBJ__PRODUCT Null
            {
                get
                {
                    var obj = new OBJ__PRODUCT { IsNull = true };
                    return obj;
                }
            }
        }


        [OracleCustomTypeMapping("OBJ__PRODUCT")]
        public class OBJ__PRODUCT_Factory : IOracleCustomTypeFactory
        {
            public IOracleCustomType CreateObject()
            {
                return new OBJ__PRODUCT();
            }
        }

        #endregion
```

#### The usage

I used [AutoMapper](https://automapper.org/) to convert between Product to OBJ__Product
but it can be done manually.

```c#
public async Task SaveProductsByExpiries(List<Product> products, DateTime expiryDate)
{
        try
        {
             await using var context = new ModelContext(_connectionString);
    
             var tmp = new ModelContext.TT__PRODUCT
             {
                  OBJ__PRODUCT = products.Select(_mapper.Map<ModelContext.OBJ__PRODUCT>).ToArray()
             };
             var retVal = await context.USP__PRODUCT_UPSERT_Async(tmp, expiryDate);
         }
         catch (Exception e)
         {
             _logger.LogError(e, "Problem SaveProductsByDates");
             throw;
         }
}
```    


## CLI Parameters

| Parameter| ShortName | Required | Description |
| --- | --- | ---| ---|
| `connection` | `c` | &#9745;|OracleServer ConnectionString |
| `schema` | `h` | &#9745;|Oracle Schema to filter Stored Procedure  |
| `namespace` | `n` |&#9745; |Namespace of the original context file  |
| `context` | `x` | &#9745;|Oracle Entity Framework dbContext from where the new files will extend|
| `sfolder` | `S` | &#9745;|Relative Solution folder where the output file will be added|
| `pfolder` | `P` | &#9745;|Physical destination folder where the output file will be written |
| `filename` | `F` | &#9745;|Output file name |
| `mode` | `M` | &#9744;|can be StoreProcedures, UDDT or ALL the default ALL |

## Compatibility

Oracle Database 19c Enterprise Edition Release 19.0.0.0.0 - Production
Version 19.11.0.0.0

## License

MIT

